﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CefSharpLiveTV
{
    class LiveTVChannel
    {
        public string[] name = new string[256];
        public string[] url = new string[256];
        public byte size = 0;
        public byte now = 0;
        public bool GetChannel()
        {
            now = 0;
            size = 0;
            Byte[] dat;
            WebClientEx webClient = new WebClientEx();
            try
            {
                webClient.Timeout = 20000;
                dat = webClient.DownloadData("https://live.wasu.cn/");
            }
            catch
            {
                webClient.Dispose();
                return false;
            }
            string src = Encoding.GetEncoding("utf-8").GetString(dat).Replace("\n", "").Replace("\r", "");
            src = src.Substring(src.IndexOf("<div class=\"tvrow\">"));
            src = src.Substring(0, src.IndexOf("</div>"));
            //<li class="pdinfo.*?<a href="//(?<url>.*?)".*?title="(?<name>.*?)".*?class="tvinfo">
            string pattern = "<li class=\"pdinfo.*?<a href=\"//(?<url>.*?)\".*?title=\"(?<name>.*?)\".*?class=\"tvinfo\">";
            Match chs = Regex.Match(src, pattern);
            if (chs.Success)
            {
                try
                {
                    webClient.Timeout = 20000;
                    dat = webClient.DownloadData("https://" + chs.Groups["url"].Value);
                }
                catch
                {
                    webClient.Dispose();
                    return false;
                }
                src = Encoding.GetEncoding("utf-8").GetString(dat).Replace("\n", "").Replace("\r", "");
                //<div class="change_item block">
                src = src.Substring(src.IndexOf("<div class=\"change_item block\">"));
                src = src.Substring(0, src.IndexOf("<script type=\"text/javascript\">"));
                //<li>.*?<a href="//(?<url>.*?)".*?/>(?<name>.*?)<.*?</li>
                pattern = "<li>.*?<a href=\"//(?<url>.*?)\".*?/>(?<name>.*?)<.*?</li>";
                chs = Regex.Match(src, pattern);
                while (chs.Success)
                {
                    //MessageBox.Show(chs.Groups["url"].Value, chs.Groups["name"].Value);
                    //频道名称
                    name[size] = chs.Groups["name"].Value.Replace(" ", "");
                    //播放地址
                    url[size] = "https://" + chs.Groups["url"].Value;
                    if (size != 255)
                    {
                        size++;
                    }
                    chs = chs.NextMatch();
                }
                if (size == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
