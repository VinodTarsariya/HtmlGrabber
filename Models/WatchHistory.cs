using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlGrabber.Models
{
    class WatchHistory
    {
        int ID { get; set; }
        int WebsiteId { get; set; }
        DateTime DateAndTime { get; set; }
        int ViewersCount { get; set; }
    }
}
