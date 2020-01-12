#ifndef INCLUDE_PATH_UTIL_H
#define INCLUDE_PATH_UTIL_H

#include <functional>

namespace filesystem_
{
// Extract from Boost.Filesystem(1.64.0) www.boost.org/libs/filesystem
// "Use, modification, and distribution are subject to the Boost Software License, Version 1.0. See www.boost.org/LICENSE_1_0.txt"
class path
{
public:
	//  -----  constructors  -----
	path() {}
	path(const wchar_t* s) : m_pathname(s) {}
	path(const wstring& s) : m_pathname(s) {}
	//  -----  concatenation  -----
	path& concat(const wchar_t* source) { m_pathname += source; return *this; }
	path& concat(const wstring& source) { m_pathname += source; return *this; }
	//  -----  appends  -----
	path& append(const wchar_t* source);
	path& append(const wstring& source);
	//  -----  modifiers  -----
	void clear() { m_pathname.clear(); }
	path& remove_filename();
	path& replace_filename(const path& p) { return remove_filename().append(p.native()); }
	path& replace_extension(const path& new_extension = path());
	//  -----  observers  -----
	const wstring& native() const { return m_pathname; }
	const wchar_t* c_str() const { return m_pathname.c_str(); }
	//  -----  decomposition  -----
	path root_path() const { return root_name().concat(root_directory().native()); }
	path root_name() const;
	path root_directory() const;
	path parent_path() const;
	path filename() const;
	path stem() const;
	path extension() const { return path(filename().c_str() + stem().native().size()); }
	//  -----  query  -----
	bool empty() const { return m_pathname.empty(); }
	bool has_root_path() const { return has_root_directory() || has_root_name(); }
	bool has_root_name() const { return !root_name().empty(); }
	bool has_root_directory() const { return !root_directory().empty(); }
	bool has_parent_path() const { return !parent_path().empty(); }
	bool has_filename() const { return !m_pathname.empty(); }
	bool has_stem() const { return !stem().empty(); }
	bool has_extension() const { return !extension().empty(); }
	bool is_relative() const { return !is_absolute(); }
#ifdef _WIN32
	bool is_absolute() const { return has_root_name() && has_root_directory(); }
	static const wchar_t preferred_separator = L'\\';
#else
	bool is_absolute() const { return has_root_directory(); }
	static const wchar_t preferred_separator = L'/';
#endif
private:
	wstring m_pathname; // Windows: as input; backslashes NOT converted to slashes,
	                    // slashes NOT converted to backslashes
	size_t m_append_separator_if_needed();
	void m_erase_redundant_separator(size_t sep_pos);
	size_t m_parent_path_end() const;
	static bool is_root_separator(const wstring& str, size_t pos);
	static size_t filename_pos(const wstring& str);
	static size_t root_directory_start(const wstring& str, size_t size);
	static void first_element(const wstring& src, size_t& element_pos, size_t& element_size);
#ifdef _WIN32
	static const wchar_t* separators() { return L"/\\"; }
	static bool is_dir_sep(wchar_t c) { return c == L'/' || c == L'\\'; }
	static bool is_letter(wchar_t c) { return (c >= L'a' && c <= L'z') || (c >= L'A' && c <= L'Z'); }
#else
	static const wchar_t* separators() { return L"/"; }
	static bool is_dir_sep(wchar_t c) { return c == L'/'; }
#endif
};
}

typedef filesystem_::path fs_path;
//#include <filesystem>
//typedef std::experimental::filesystem::path fs_path;

enum {
	UTIL_O_RDONLY = 1, // r
	UTIL_O_RDWR = 3, // r+
	UTIL_O_CREAT_WRONLY = 10, // w
	UTIL_O_CREAT_RDWR = 11, // w+
	UTIL_O_CREAT_APPEND = 22, // a
	UTIL_O_EXCL_CREAT_WRONLY = 26, // wx
	UTIL_O_EXCL_CREAT_RDWR = 27, // w+x
	UTIL_O_EXCL_CREAT_APPEND = 30, // ax
	UTIL_SH_READ = 32,
	UTIL_SH_DELETE = 128,
	UTIL_F_SEQUENTIAL = 256, // S
	UTIL_F_IONBF = 512, // setvbuf(_IONBF)
	UTIL_SECURE_WRITE = UTIL_O_CREAT_WRONLY, // fopen_s(w)
	UTIL_SECURE_READ = UTIL_O_RDONLY | UTIL_SH_READ, // fopen_s(r)
	UTIL_SHARED_READ = UTIL_O_RDONLY | UTIL_SH_READ | 64, // fopen(r)
};

// �t�@�C�����J��(�p���s�\�A���L���[�h�����)
FILE* UtilOpenFile(const wstring& path, int flags);
inline FILE* UtilOpenFile(const fs_path& path, int flags) { return UtilOpenFile(path.native(), flags); }

#ifndef _WIN32
BOOL DeleteFile(LPCWSTR path);
#endif

fs_path GetDefSettingPath();
fs_path GetSettingPath();
#ifdef _WIN32
fs_path GetModulePath(HMODULE hModule = NULL);
fs_path GetModuleIniPath(HMODULE hModule = NULL);
#else
fs_path GetModulePath();
fs_path GetModuleIniPath(LPCWSTR moduleName = NULL);
#endif
fs_path GetCommonIniPath();
fs_path GetRecFolderPath(int index = 0);
int UtilComparePath(LPCWSTR path1, LPCWSTR path2);
bool UtilPathEndsWith(LPCWSTR path, LPCWSTR suffix);
void CheckFileName(wstring& fileName, bool noChkYen = false);

#ifdef _WIN32
// ���݂��Ȃ����BOM�t���̋�t�@�C�����쐬����
void TouchFileAsUnicode(const fs_path& path);
#endif
// �t�@�C���̑��݂��m�F����B���ꂪ�f�B���N�g���łȂ���Α�2�Ԓl��true
pair<bool, bool> UtilFileExists(const fs_path& path, bool* mightExist = NULL);
bool UtilCreateDirectory(const fs_path& path);
// �ċA�I�Ƀf�B���N�g���𐶐�����
bool UtilCreateDirectories(const fs_path& path);
// �t�H���_������X�g���[�W�̋󂫗e�ʂ��擾����B���s���͕��l
__int64 UtilGetStorageFreeBytes(const fs_path& directoryPath);
// �t�H���_������X�g���[�W�̎��ʎq���擾����B���s���͋�
wstring UtilGetStorageID(const fs_path& directoryPath);

#ifdef _WIN32
// �K�v�ȃo�b�t�@���m�ۂ���GetPrivateProfileSection()���Ă�
vector<WCHAR> GetPrivateProfileSectionBuffer(LPCWSTR appName, LPCWSTR fileName);
#else
int GetPrivateProfileInt(LPCWSTR appName, LPCWSTR keyName, int nDefault, LPCWSTR fileName);
BOOL WritePrivateProfileString(LPCWSTR appName, LPCWSTR keyName, LPCWSTR lpString, LPCWSTR fileName);
#endif
wstring GetPrivateProfileToString(LPCWSTR appName, LPCWSTR keyName, LPCWSTR lpDefault, LPCWSTR fileName);
BOOL WritePrivateProfileInt(LPCWSTR appName, LPCWSTR keyName, int value, LPCWSTR fileName);

struct UTIL_FIND_DATA {
	bool isDir;
	__int64 lastWriteTime;
	__int64 fileSize;
	wstring fileName;
};

// FindFirstFile()�̌��ʂ�񋓂���
void EnumFindFile(const fs_path& pattern, const std::function<bool(UTIL_FIND_DATA&)>& enumProc);

#endif
