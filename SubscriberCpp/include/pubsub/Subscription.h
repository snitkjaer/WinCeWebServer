#pragma once

class Subscription
{
public:
	char ClientId[50];
	char SubscribedEvent[100];

	Subscription(){}
	Subscription(const char* clientId, const char* subscribedEvent)
	{
		strcpy(this->ClientId, clientId);
		strcpy(this->SubscribedEvent, subscribedEvent);
	}
};
