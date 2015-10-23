#pragma once
#include "resourceppc.h"

void LogW(TCHAR* message);
void Log(const char* message);
void CharToTChar(TCHAR* dest, const char* src, size_t len);
void OnWindowResize(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);
void OnPublishClick();
void OnHandleResponse();
void WaitForResponse(bool wait);
void GetText(char* buf, int size, int id);