/* 头文件　*/
#include <windows.h>
#include <tchar.h>
#include <stdio.h>
/* 预定义　*/
#define BUFSIZE 4096
/*************************************
* DWORD WINAPI EnumEnvironmentVariables()
* 功能	显示进程的所有环境变量
**************************************/
DWORD WINAPI EnumEnvironmentVariables()
{
	// 获取环境变量
	LPWCH pEv = GetEnvironmentStrings();
	LPSTR szEnvs;
	// 显示
	for (szEnvs = (LPSTR) pEv; *szEnvs;) 
	{ 
		printf("%s\n",szEnvs);
		while (*szEnvs++);
	}
	// 释放
	FreeEnvironmentStrings(pEv);
	return 0;
}

enum VARIABLES_CONTROL
{
	VARIABLES_APPEND = 0,
	VARIABLES_RESET,
	VARIABLES_NULL
};
/*************************************
* DWORD WINAPI ChangeEnviromentVariables(LPSTR szName, 
LPSTR szNewValue, 
DWORD dwFlag)
* 功能	改变环境变量
*
* 参数	LPSTR szName	需要改变的环境
*		LPSTR szNewValue	新的变量值
*		DWORD dwFlag	附加、重置还是清零
**************************************/
DWORD WINAPI ChangeEnviromentVariables(LPTSTR szName, 
									   LPTSTR szNewValue, 
									   DWORD dwFlag)
{
	DWORD dwErr;
	DWORD dwReturn; 
	DWORD dwNewValSize;
	// 如果标志为附加则则先获取，然后将szNewValue附加到末尾
	if(dwFlag == VARIABLES_APPEND)
	{
		dwNewValSize = lstrlen(szNewValue)+1;	// 新变量值的大小
		// 分配内存
		LPWSTR szVal = (LPWSTR)HeapAlloc(GetProcessHeap(),0,BUFSIZE+dwNewValSize);
		// 获取值
		dwReturn = GetEnvironmentVariable(szName,szVal,BUFSIZE);
		if(dwReturn == 0)	// 出错
		{
			dwErr = GetLastError();
			if( ERROR_ENVVAR_NOT_FOUND == dwErr )
			{
				printf("Environment variable %s does not exist.\n", szName);
			}
			else
			{
				printf("error: %d",dwErr);
			}
			return FALSE;
		}
		else if(BUFSIZE < dwReturn)	// 缓冲区太小
		{
			szVal = (LPTSTR) HeapReAlloc(GetProcessHeap(), 0,szVal, dwReturn+dwNewValSize);
			if(NULL == szVal)
			{
				printf("Memory error\n");
				return FALSE;
			}
			dwReturn = GetEnvironmentVariable(szName, szVal, dwReturn);
			if(!dwReturn)
			{
				printf("GetEnvironmentVariable failed (%d)\n", GetLastError());
				return FALSE;
			}
		}
		lstrcat(szVal, _T (";"));		// 分隔符
		lstrcat(szVal,szNewValue);	// 附加
		//设置
		if(!SetEnvironmentVariable(szName,szVal))
		{
			printf("Set Value Error %d",GetLastError());
		}
		// 释放内存
		HeapFree(GetProcessHeap(),0,szVal);
		return TRUE;
	}
	// 如果是重置，则直接设置
	else if(dwFlag == VARIABLES_RESET)
	{
		if(!SetEnvironmentVariable(szName,szNewValue))
		{
			printf("Set value error %d",GetLastError());
		}
	}
	// 清零，忽略szNewValue
	else if(dwFlag == VARIABLES_NULL)
	{
		if(!SetEnvironmentVariable(szName,NULL))
		{
			printf("Set value error %d",GetLastError());
		}
	} 
	return TRUE;
}