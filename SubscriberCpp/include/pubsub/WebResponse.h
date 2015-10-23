#pragma once

class WebResponse 
{
public:
	short Status;
	char StatusCode[10];
	char Protocol[20];
	char ContentType[50];
	long ContentLength;
	char* Body;
	
	void Parse(const char* responseText);
	~WebResponse();
};