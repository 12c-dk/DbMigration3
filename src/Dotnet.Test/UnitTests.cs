using DbMigration.Common.Legacy.Extensions;
using DbMigration.Common.Legacy.Helpers.DictionaryHelpers;
using DbMigration.Common.Legacy.Model.MappingModel;
using Xunit.Abstractions;

namespace Dotnet.Test
{
    public class UnitTests
    {
        readonly ITestOutputHelper _log;

        public UnitTests(ITestOutputHelper log)
        {
            _log = log;
        }


        [Fact]
        public void CollectionIntersectTest()
        {
            //Get items has same ids in both lists. return the full target items

            // Example source table
            var sourceTable = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { {"ID", 1},{ "ID2",1000 }, {"Name", "Apple"} },
            new Dictionary<string, object> { {"ID", 2},{ "ID2", 2000 }, { "Name", "Banana"} }
        };

            // Example target table
            var targetTable = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { {"ID", 1}, { "ID2", 1000 }, { "Name", "Mango"} },
                new Dictionary<string, object> { {"ID", 2}, { "ID2", 2000 }, { "Name", "Banana"} },
                new Dictionary<string, object> { {"ID", 3}, { "ID2", 3000 }, { "Name", "Cherry"} }
            };

            // Use the custom comparer for dictionary comparison
            var matches = targetTable.Intersect(
                sourceTable,
                new DictionaryComparer(new List<string>() { "ID", "ID2" }));

            // Display matching rows
            foreach (var row in matches)
            {
                _log.WriteLine($"Matching Row: ID = {row["ID"]}, Name = {row["Name"]}");
            }

        }


        [Fact]
        public void FindMatchingRowMapTest()
        {
            //Get items has same ids in both lists. Returns a dictionary of the mapped source and target items. 

            var sourceTable = new List<DbItem>
            {
                new DbItem { {"ID", 1},{ "ID2",1000 }, {"Name", "Apple"} },
                new DbItem { {"id", 2},{ "Id2", 2000 }, { "Name", "Banana"} },
                new DbItem { {"id", 4},{ "Id2", 4000 }, { "Name", "Orange"} },
                //new DictionaryCaseInsensitive<object> { {"id", 2},{ "Id2", 2000 }, { "Name", "Jack fruit"} }
            };

            // Example target table
            var targetTable = new List<DbItem>
            {
                new DbItem { {"ID", 1}, { "ID2", 1000 }, { "Name", "Mango"} },
                new DbItem { {"ID", 2}, { "ID2", 2000 }, { "Name", "Banana"} },
                new DbItem { {"ID", 3}, { "ID2", 3000 }, { "Name", "Cherry"} },
                //new DictionaryCaseInsensitive<object> { {"ID", 2}, { "ID2", 2000 }, { "Name", "Cucumber"} }
            };
            List<string> compareKeys = new List<string>() { "ID", "ID2" };

            // Find matching rows and map them
            DbValueMapOperationResponse<DbItem> matchingRowsResponse = FindRowsMap.FindMatchingRowMap(sourceTable, targetTable, compareKeys);
            var matchingRowMap = matchingRowsResponse.ResponseValue;
            var noMatchItems = matchingRowsResponse.NoMatchItems;

            // Print matching row map
            _log.WriteLine("Matching Items");
            foreach (var entry in matchingRowMap)
            {
                _log.WriteLine($"Source Row: {{ id = {entry.Key["ID"]}, id2 = {entry.Key["ID2"]}, Name = {entry.Key["Name"]} }}");
                _log.WriteLine($"Target Row: {{ id = {entry.Value["ID"]}, id2 = {entry.Value["ID2"]}, Name = {entry.Value["Name"]} }}");
            }

            _log.WriteLine("No Match Items");
            foreach (DbItem entry in noMatchItems)
            {
                _log.WriteLine($"Source Row: {{ id = {entry["ID"]}, id2 = {entry["ID2"]}, Name = {entry["Name"]} }}");
            }

        }

        [Fact]
        public void CollectionIsUniqueTest()
        {
            //Check if source items and values are unique by compare keys

            List<string> compareKeys = new List<string>() { "ID", "ID2" };
            var sourceTable = new List<DictionaryCaseInsensitive<object>>
            {
                new DictionaryCaseInsensitive<object> { {"ID", 1},{ "ID2",1000 }, {"Name", "Apple"} },
                new DictionaryCaseInsensitive<object> { {"id", 2},{ "ID2", 2000 }, { "Name", "Banana"} },
                new DictionaryCaseInsensitive<object> { {"id", 2},{ "ID2", 2000 }, { "Name", "Orange" } }
            };

            bool areUnique = FindRowsMap.CollectionKeysAreUnique(sourceTable, compareKeys);

            var distinctObjects = sourceTable.Distinct(new EqualityComparerByJson<DictionaryCaseInsensitive<object>>());

            Assert.False(areUnique);
            Assert.NotNull(distinctObjects);


        }

        /// <summary>
        /// Testing Debug info on hover when investigating generic tables
        /// </summary>
        [Fact]
        public void DictionaryDebugTest()
        {
            //Bad
            DictionaryCaseInsensitive<object> dict = new DictionaryCaseInsensitive<object>
            {
                {"Name", "Filip"},
                {"Age", 42}
            };
            List<DictionaryCaseInsensitive<object>> list1 = new List<DictionaryCaseInsensitive<object>> {dict};

            //Good
            List<KeyValuePair<string, object>[]> kv2 = list1.ToKeyValueList();
            Assert.NotNull(kv2); // Add assert on kv2

            //Good
            KeyValuePair<string, object>[] dictArray2 = dict.ToArray();
            List<KeyValuePair<string, object>[]> list3 = new List<KeyValuePair<string, object>[]> {dictArray2};
            Assert.Single(list3); // Add assert on list3
        }
        

    }


}