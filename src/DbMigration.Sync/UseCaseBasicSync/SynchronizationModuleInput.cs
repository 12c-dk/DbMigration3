using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbMigration.Sync.UseCaseBasicSync
{
    public class SynchronizationModuleInput
    {
        public string SourceConnectionName { get; set; }
        public string TargetConnectionName { get; set; }
        public string SourceTable { get; set; }
        public string TargetTable { get; set; }

    }
}
