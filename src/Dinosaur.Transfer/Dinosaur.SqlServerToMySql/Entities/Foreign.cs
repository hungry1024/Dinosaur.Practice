namespace Dinosaur.SqlServerToMySql.Entities
{
    public class Foreign
    {
        public string ForeignName { get; set; }

        public string KeyTable { get; set; }

        public string KeyColumn { get; set; }

        public string ForeignTable { get; set; }

        public string ForeignKey { get; set; }
    }
}
