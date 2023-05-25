namespace AgileConfig.Client.Utils
{
    internal class AssemblyUtil
    {
        public static string GetVer()
        {
            var type = typeof(AssemblyUtil);
            var ver = type.Assembly.GetName().Version;

            return $"{ver.Major}.{ver.Minor}.{ver.Build}";
        }
    }
}
