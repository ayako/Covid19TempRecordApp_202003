using Microsoft.Azure.Cosmos.Table;

namespace Covid19TempRecordApp_202003
{
    public class UserEntity : TableEntity
    {
        public UserEntity()
        { }

        public double Temperature { get; set; }

        public string Memo { get; set; }
    }
}
