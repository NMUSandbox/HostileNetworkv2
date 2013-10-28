using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HostileNetworkUtils {
    public class Utils {

        public static bool fileExists(string fileName) {

            return File.Exists(fileName);
        }
    }
}
