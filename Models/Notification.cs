namespace NihongoHelperBot.DBContext
{
    class Notification
    {
        public int Id { get; set; }
        public int Id_User { get; set; }
        public string Timeout { get; set; }
        public bool Is_Notificated { get; set; }
        public string NextNotify { get; set; }
    }
}
