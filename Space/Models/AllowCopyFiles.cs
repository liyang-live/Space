using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Space.Models
{
    [DataContract]
    public class Allowcopyfiles
    {
        [DataMember]
        public List<Allowcopyfile> AllowCopyFiles { get; set; }

        [DataMember]
        public List<AllowcopyFolder> AllowCopyFolders { get; set; }
    }

    [DataContract]
    public class Allowcopyfile
    {
        [DataMember]
        public string FileName { get; set; }
    }


    [DataContract]
    public class AllowcopyFolder
    {
        [DataMember]
        public string FolderName { get; set; }
    }

}
