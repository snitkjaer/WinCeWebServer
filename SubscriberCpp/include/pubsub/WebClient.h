#pragma once

#include "winsock2.h"
#include "WebResponse.h"

class WebClient {

public:
	bool IsConnected;
	WebClient(const char* hostname, int port);
	int SendGet(const char* action);
	int SendPost(const char* action, const char* data);
	int Connect();
	//int ConnectAsync(HWND hwnd);
	void (*WebClientLog)(const char* msg);
	void CloseConnection();
	WebResponse* WebClient::GetResponse();
	~WebClient();

private:
	SOCKET _sock;
	SOCKADDR_IN _source;
	char _hostName[100];
	int _port;
	void _Log(char* msg);
	char* _request;
	char* _BuildHttpRequest(const char* method, const char* path, const char* data);
};