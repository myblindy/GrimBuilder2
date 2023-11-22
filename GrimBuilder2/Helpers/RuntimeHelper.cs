using System.Runtime.InteropServices;
using System.Text;

namespace GrimBuilder2.Helpers;

public partial class RuntimeHelper
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial int GetCurrentPackageFullName(ref int packageFullNameLength, IntPtr packageFullName);

    public static bool IsMSIX
    {
        get
        {
            var length = 0;

            return GetCurrentPackageFullName(ref length, IntPtr.Zero) != 15700L;
        }
    }
}
