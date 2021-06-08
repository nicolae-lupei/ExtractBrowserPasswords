using System.ComponentModel.DataAnnotations.Schema;

namespace ExtractCookiesFromBrowser.Models
{
    internal class LoginData
    {
        [Column("origin_url")]
        public string OriginUrl { get; set; }

        [Column("action_url")]
        public string ActionUrl { get; set; }

        [Column("username_value")]
        public string UsernameValue { get; set; }

        [Column("password_value")]
        public byte[] PasswordValue { get; set; }

        [Column("date_created")]
        public long DateCreated { get; set; }

        [Column("date_last_used")]
        public long DateLastUsed { get; set; }
    }
}