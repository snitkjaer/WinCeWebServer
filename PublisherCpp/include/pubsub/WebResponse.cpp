#include "stdafx.h"
#include "WebResponse.h"

void ParseHeaderLine(const char* line, char* key, char* value)
{
	const char* pos = strchr(line, (int)':');
	if (!pos)
		return;

	int n = strlen(line);
	std::ostringstream buf;

	for(int i = 0; i < n; i++)
	{
		if (line[i] != ' ' && line[i] != (int)':')
			buf << line[i];

		if (line[i] == (int)':') 
		{
			strcpy(key, buf.str().data());
			buf.str("");
		}
	}

	if (buf.str().length() >= 0)
	{
		strcpy(value, buf.str().data());
	}
}

WebResponse::~WebResponse()
{
	if (this->Body)
		GlobalFree((HANDLE)this->Body);
}

void WebResponse::Parse(const char *responseText)
{
	int len = strlen(responseText);
	int index = 0;
	const char* tmp;
	int tmp_len = 0;
	std::ostringstream buf;
	bool finishReadHeader = false;
	int contentLength = 0;
	char hkey[100];
	char hval[100];

	// init
	memset(this->Protocol, 0, sizeof(this->Protocol));
	memset(this->StatusCode, 0, sizeof(this->StatusCode));
	memset(this->ContentType, 0, sizeof(this->ContentType));
	this->Status = 0;
	this->ContentLength = 0;

	while (index < len)
	{
		if (strlen(this->Protocol) == 0)
		{
			if (responseText[index] != ' ')
                buf << responseText[index];
            else
            {
				strcpy(this->Protocol, buf.str().data());
                buf.str("");
            }

            index++;
            continue;
		}

		if (this->Status <= 0)
		{
			if (responseText[index] != ' ')
                buf << responseText[index];
            else
            {
				this->Status = atoi(buf.str().data());
                buf.str("");
            }

            index++;
            continue;
		}

		if (strlen(this->StatusCode) == 0)
		{
			if (responseText[index] != '\r' && responseText[index] != '\n')
                buf << responseText[index];
            else if (responseText[index] == '\n')
            {
				strcpy(this->StatusCode, buf.str().data());
                buf.str("");
            }

            index++;
            continue;
		}

		if (!finishReadHeader)
        {
            if (responseText[index] != '\r' && responseText[index] != '\n')
                buf << responseText[index];
            else if (responseText[index] == '\n')
            {
				finishReadHeader = buf.str().length() == 0;
                if (!finishReadHeader)
                {
					memset(hkey, 0, sizeof(hkey));
					memset(hval, 0, sizeof(hval));
					ParseHeaderLine(buf.str().data(), hkey, hval);

					if (strstr((const char*)hkey, "Content-Length"))
					{
						this->ContentLength = atoi(hval);
					}
					else if (strstr((const char*)hkey, "Content-Type"))
					{
						strcpy(this->ContentType, (const char*)hval);
					}
                }
				else if (this->ContentLength > 0)
                {
					this->Body = (char*)GlobalAlloc(GPTR, this->ContentLength + 1);
                }

                buf.str("");
            }

            index++;
            if (!finishReadHeader)
                continue;   
        }

		// if we make it this far, which means all header is read
        buf << responseText[index++];
	}

	if (buf.str().length() > 0 && this->ContentLength > 0)
	{
		strncpy(this->Body, buf.str().data(), this->ContentLength);
	}
}
