#include <Windows.h>
#include <string>

const wchar_t wDisk[22] = L"\\\\.\\PhysicalDrive0";
const wchar_t wDrev[10]  = L"\\\\.\\A:";


//读盘
BOOL WINAPI Disk_ReadSectors(int iDisk, DWORD dwStart, DWORD dwStartHigh, DWORD dwOffsetSectors, DWORD dwSize, LPBYTE lpReadBuff)
{
	//整理数据
	unsigned __int64 _64OffsetBase = (dwStart + dwStartHigh*0x100000000)*512+dwOffsetSectors;
	dwStart = (DWORD)_64OffsetBase;
	dwStartHigh = (DWORD)(_64OffsetBase/0x100000000);

	//初始化字符串参数
	std::wstring wBuff = ::wDisk;
	wBuff[17] = (wchar_t)(iDisk + L'0');

	//开始读盘
	HANDLE hDisk = CreateFileW(wBuff.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if(INVALID_HANDLE_VALUE == hDisk)
		return FALSE;
	SetFilePointer(hDisk, dwStart, (PLONG)&dwStartHigh, FILE_BEGIN);
	BOOL bRet = ReadFile(hDisk, lpReadBuff, dwSize, &dwOffsetSectors, NULL);
	CloseHandle(hDisk);
	return bRet;
}


//写盘
BOOL WINAPI Disk_WriteSectors(int iDisk, DWORD dwStart, DWORD dwStartHigh, DWORD dwOffsetSectors, DWORD dwSize, LPBYTE lpWriteBuff)
{
	//整理数据
	unsigned __int64 _64OffsetBase = (dwStart + dwStartHigh*0x100000000)*512+dwOffsetSectors;
	dwStart = (DWORD)_64OffsetBase;
	dwStartHigh = (DWORD)(_64OffsetBase/0x100000000);

	//初始化字符串参数
	std::wstring wBuff = ::wDisk;
	wBuff[17] = (wchar_t)(iDisk + L'0');

	//开始写盘
	HANDLE hDisk = CreateFileW(wBuff.c_str(), GENERIC_WRITE, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if(INVALID_HANDLE_VALUE == hDisk)
		return FALSE;
	SetFilePointer(hDisk, dwStart, (PLONG)&dwStartHigh, FILE_BEGIN);
	BOOL bRet = WriteFile(hDisk, lpWriteBuff, dwSize, &dwOffsetSectors, NULL);
	CloseHandle(hDisk);
	return bRet;
}


//读驱动器
BOOL WINAPI Drev_ReadSectors(wchar_t wDrev, DWORD dwStart, DWORD dwStartHigh, DWORD dwOffsetSectors, DWORD dwSize, LPBYTE lpReadBuff)
{
	//整理数据
	unsigned __int64 _64OffsetBase = (dwStart + dwStartHigh*0x100000000)*512+dwOffsetSectors;
	dwStart = (DWORD)_64OffsetBase;
	dwStartHigh = (DWORD)(_64OffsetBase/0x100000000);

	//初始化字符串参数
	std::wstring wBuff = ::wDrev;
	wBuff[4] = wDrev;

	//开始读盘
	HANDLE hDrev = CreateFileW(wBuff.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
	if(INVALID_HANDLE_VALUE == hDrev)
		return FALSE;
	SetFilePointer(hDrev, dwStart, (PLONG)&dwStartHigh, FILE_BEGIN);
	BOOL bRet = ReadFile(hDrev, lpReadBuff, dwSize, &dwOffsetSectors, NULL);
	CloseHandle(hDrev);
	return bRet;
}


//写驱动器
BOOL WINAPI Drev_WriteSectors(wchar_t wDrev, DWORD dwStart, DWORD dwStartHigh, DWORD dwOffsetSectors, DWORD dwSize, LPBYTE lpWriteBuff)
{
	//整理数据
	unsigned __int64 _64OffsetBase = (dwStart + dwStartHigh*0x100000000)*512+dwOffsetSectors;
	dwStart = (DWORD)_64OffsetBase;
	dwStartHigh = (DWORD)(_64OffsetBase/0x100000000);

	//初始化字符串参数
	std::wstring wBuff = ::wDrev;
	wBuff[4] = wDrev;

	//开始写盘
	HANDLE hDrev = CreateFileW(wBuff.c_str(), GENERIC_WRITE, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
	if(INVALID_HANDLE_VALUE == hDrev)
		return FALSE;
	SetFilePointer(hDrev, dwStart, (PLONG)&dwStartHigh, FILE_BEGIN);
	BOOL bRet = WriteFile(hDrev, lpWriteBuff, dwSize, &dwOffsetSectors, NULL);
	CloseHandle(hDrev);
	return bRet;
}
