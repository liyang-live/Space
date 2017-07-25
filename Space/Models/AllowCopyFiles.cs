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
        public List<Allowcopyfile> ClientAllowCopyFiles { get; set; }

        [DataMember]
        public List<AllowcopyFolder> ClientAllowCopyFolders { get; set; }

        [DataMember]
        public List<Allowcopyfile> ServerAllowCopyFiles { get; set; }

        [DataMember]
        public List<AllowcopyFolder> ServerAllowCopyFolders { get; set; }
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
