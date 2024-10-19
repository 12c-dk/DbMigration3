namespace DbMigration.Common.Legacy.Model
{
    public static class Constants
    {
        public static class Tables
        {
            public static class DbConnections
            {
                public const string Name = "dbconnections";
                public const string PartitionKey = "dbconnection";
            }

            public static class Projects
            {
                public const string Name = "Projects";
                public const string PartitionKey = "project";
            }
        }

        public static class Queues
        {
            public const string Load = "load";
            public const string Transform = "transform";
        }
    }
}
