using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlGrabber.Models
{
    public class WebsiteHelper
    {        
        int ID { get; set; }
        string WebsiteLink { get; set; }
        string FindRegex { get; set; }
        int Match { get; set; }
        string Remark { get; set; }
        int RefreshTime { get; set; }
        int MaxViewerCount { get; set; }
    }
}
