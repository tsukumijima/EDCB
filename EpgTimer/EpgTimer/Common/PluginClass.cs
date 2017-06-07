using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;

namespace EpgTimer
{
    public static class PlugIns
    {
        [DllImport("kernel32", CharSet = CharSet.Auto, ExactSpelling = false)]
        private extern static IntPtr LoadLibrary(String lpFileName);

        [DllImport("kernel32", ExactSpelling = true)]
        private extern static bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private extern static IntPtr GetProcAddress(IntPtr hModule, String lpProcName);

        //Tはdelegateのタイプ。delegate名は、(任意の1文字) + dll内proc名
        private static object runProc<T>(String dllPath, Func<T, object> proc) where T : class
        {
            object ret = null;
            var module = LoadLibrary(dllPath);
            if (module != IntPtr.Zero)
            {
                try
                {
                    IntPtr _procPtr = GetProcAddress(module, typeof(T).Name.Substring(1));
                    var _proc = Marshal.GetDelegateForFunctionPointer(_procPtr, typeof(T));
                    ret = proc(_proc as T);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
                FreeLibrary(module);
            }
            return ret;
        }

        delegate void _Setting(IntPtr parentWnd);
        public static void Setting(String dllPath, IntPtr parentWnd)
        {
            runProc<_Setting>(dllPath, setting => { setting(parentWnd); return null; });
        }

        /* 未使用
        delegate UInt32 _GetPlugInName([MarshalAs(UnmanagedType.LPWStr)]StringBuilder name, ref UInt32 nameSize);
        public static String GetPlugInName(String dllPath)
        {
            UInt32 nameSize = 256;
            var name = new StringBuilder((int)nameSize);
            runProc<_GetPlugInName>(dllPath, getPlugInName => getPlugInName(name, ref nameSize));
            return name.ToString();
        }//*/
    }
}
