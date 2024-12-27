using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realm.Data
{
    public class ClassData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int HighScore { get; set; }
        public bool Locked { get; set; }
    }
}
