namespace SteamAutoLauncher.Core.SteamGuard
{
    public class MaFileData
    {
        public string? shared_secret { get; set; }
        public string? serial_number { get; set; }
        public string? revocation_code { get; set; }
        public long uri { get; set; }
        public long server_time { get; set; }
        public int account_name { get; set; }
        public int token_gid { get; set; }
        public int identity_secret { get; set; }
        public int secret_1 { get; set; }
        public string? status { get; set; }
        public int phone_number_hint { get; set; }
    }
}