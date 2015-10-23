#include "stdafx.h"
#include "WebClient.h"
#include "WebResponse.h"
#include "winsock2.h"

WebClient::WebClient(const char* hostname, int port)
{
	strcpy(this->_hostName, hostname);
	this->_port = port;
	this->IsConnected = false;
}

WebClient::~WebClient()
{
	CloseConnection();
}

void WebClient::_Log(char* msg)
{
	char errorMsg[200];
	int err = WSAGetLastError();

	if (err != 0)
		sprintf(errorMsg, "%s: %d", msg, err);
	else
		sprintf(errorMsg, "%s", msg);

	if (this->WebClientLog)
	{
		(this->WebClientLog)(errorMsg);
	}
}

int WebClient::Connect()
{
	WSADATA w; //Winsock startup info
	SOCKADDR_IN target; //information about host

	// close openning connection
	this->CloseConnection();

	if(WSAStartup(0x0202, &w))
		return 0;

	if (w.wVersion != 0x0202)
	{
		WSACleanup();
		return 0;
	}

	long host = inet_addr(this->_hostName);
	this->_sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if (this->_sock == INVALID_SOCKET)
		return 0;

	target.sin_family = AF_INET; //address family internet
	target.sin_port = htons(this->_port); // set server port, convert to network byte order
	target.sin_addr.s_addr = host; // set server ip, convert to network byte order

	// connecting...
	// this->_Log("connecting...");
	if (connect(this->_sock, (SOCKADDR*)&target, sizeof(target)) == SOCKET_ERROR)
	{
		this->_Log("connection error");
		this->CloseConnection();
		return 0;
	}

	this->IsConnected = true;
	return 1;
}

//
//int WebClient::ConnectAsync(HWND hwnd)
//{
//	int ret = this->Connect();
//	// switch to non blocking mode
//	if (ret)
//		WSAAsyncSelect(this->_sock, hwnd, 1045, FD_READ | FD_CONNECT | FD_CLOSE);
//
//	return ret;
//}

int WebClient::SendGet(const char *action)
{
	if (!this->IsConnected)
		return 0;

	if (this->_request)
		GlobalFree((HANDLE)this->_request);
	this->_request = this->_BuildHttpRequest("GET", action, "");
	int sent = send(this->_sock, this->_request, strlen(this->_request) + 1, 0);

	if (sent <= 0)
		this->_Log("send GET error");
	
	return sent;
}

int WebClient::SendPost(const char *action,  const char* data)
{
	if (!this->IsConnected)
		return 0;

	if (this->_request)
		GlobalFree((HANDLE)this->_request);
	this->_request = this->_BuildHttpRequest("POST", action, data);
	int sent = send(this->_sock, this->_request, strlen(this->_request) + 1, 0);

	if (sent <= 0)
		this->_Log("send POST error");

	return sent;
}

char* WebClient::_BuildHttpRequest(const char* method, const char* path, const char* data)
{
	int len = data != NULL ? strlen(data) : 0;
	std::ostringstream buf; 

	// header
	buf << method << " " << path << " HTTP/1.1\r\n";
	buf << "Content-Type: application/json\r\n";
	buf << "Host: \r\n";
	buf << "Content-Length: " << len;
	buf << "\r\n";

	// body
	if (len > 0)
	{
		buf << "\r\n";
		buf << data;
	}

	char* req = (char*)GlobalAlloc(GPTR, buf.str().length() + 1);
	strcpy(req, buf.str().data());
	return req;
}

void WebClient::CloseConnection()
{
	if (this->_sock) 
		closesocket(this->_sock);
	if (this->_request)
	{
		GlobalFree((HANDLE)this->_request);
		this->_request = NULL;
	}
		
	WSACleanup();
	this->IsConnected = false;
}

WebResponse* WebClient::GetResponse()
{
	char buf[8000];
	std::stringstream ss;
	int read = 0;

	do
	{
		memset(buf, 0, sizeof(buf));
		read = recv(this->_sock, buf, sizeof(buf), 0);
		if (read > 0)
			ss << buf;
	} while(read > 0);

	this->CloseConnection();

	if (ss.str().length() <= 0)
		return NULL;

	WebResponse* res = new WebResponse();
	res->Parse(ss.str().data());
	return res;
}