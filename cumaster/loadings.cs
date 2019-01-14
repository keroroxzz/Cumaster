using System;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using BHforumAPI;
using static global.funcs;

namespace baha_test
{
    public class Step
    {
        public string name;
        public int x, y, id;

        public Step(string s, string xx, string yy)
        {
            id = -1;
            name = s;
            x = Convert.ToChar(xx)-65;
            y = Convert.ToChar(yy)-65;
        }
    }
    
    public partial class MainWindow : Window
    {
        //load BH API
        BHforumAPI.BHforumAPI bh = new BHforumAPI.BHforumAPI();

        //HttpClient
        HttpClient HC = new HttpClient();

        Regex rxUrl = new Regex(@"bsn=(?<bsn>\d+)\D+snA=(?<sna>\d+)", RegexOptions.IgnoreCase),
         command = new Regex(@"-(?<x>[A-T])(?<y>[A-T])-"),
         CommandForText = new Regex(@"(?<n>\w+) -(?<x>[A-T])(?<y>[A-T])-"),
         room = new Regex(@"roomid=(?<roomid>\d+)", RegexOptions.IgnoreCase);

        List<Comment> FindComment(Topic topic, String floortext)
        {
            if (topic != null && floortext != "")
            {
                uint floor = Convert.ToUInt16(floortext);

                //load topic
                List<Post> p = topic.GetPosts(floor / 20 + 1);

                foreach (Post i in p)
                    if (i.Floor == floor)
                        return i.GetComments();
            }
            return null;
        }

        List<Step> Extract(List<Comment> comments, String roomidtext)
        {
            string[] names = { "", "" };

            int startIndex = 0;

            if (comments == null)
                return null;

            for (int i = 0; i < comments.Count; i++)
            {
                Match m = room.Match(comments[i].comment);
                if (m.Success && m.Groups["roomid"].Value == roomidtext)
                {
                    if (names[0] == "")
                    {
                        startIndex = i;
                        names[0] = comments[i].nick;
                    }
                    else if (names[1] == "")
                    {
                        names[1] = comments[i].nick;
                        break;
                    }
                }
            }
            if (names[0] != "")
            {
                List<Step> stepList = new List<Step>();

                for (int i = startIndex; i < comments.Count; i++)
                {
                    if (comments[i].nick == names[0] || comments[i].nick == names[1])
                    {
                        Match m = command.Match(comments[i].comment);
                        if (m.Success)
                        {
                            stepList.Add(new Step(comments[i].nick, m.Groups["x"].Value, m.Groups["y"].Value));
                        }
                    }
                }
                return stepList;
            }
            else
                return null;
        }

        List<Step> Extract(String text)
        {
            string[] 
                names = { "", "" },
                list = text.Split('\n');

            int startIndex = 0;

            if (list.Length >= 0)
            {
                for (int i = 0; i < list.Length; i++)
                {
                    Match m = CommandForText.Match(list[i]);
                    if (m.Success)
                    {
                        if (names[0] == "")
                        {
                            startIndex = i;
                            names[0] = m.Groups["n"].Value;
                        }
                        else if (names[1] == "")
                        {
                            names[1] = m.Groups["n"].Value;
                            break;
                        }
                    }
                }
                if (names[0] != "")
                {
                    List<Step> stepList = new List<Step>();

                    for (int i = startIndex; i < list.Length; i++)
                    {
                        Match m = CommandForText.Match(list[i]);
                        if(m.Success)
                            if (m.Groups["n"].Value == names[0] || m.Groups["n"].Value == names[1])
                            {
                                stepList.Add(new Step(m.Groups["n"].Value, m.Groups["x"].Value, m.Groups["y"].Value));
                            }
                    }
                    return stepList;
                }
            }
            return null;
        }

        void Load(List<Step> stepList)
        {
            string[] player = { "", "" };//其實這沒啥太大用處www

            for (int i = 0; i < stepList.Count; i++)
            {
                stepList[i].id = i % 2;

                if (i == 0)
                    player[i] = stepList[i].name;

                else if (player[1] == "")
                {
                    if (stepList[i].name == stepList[0].name)
                        stepList.RemoveAt(i--);
                    else
                        player[1] = stepList[i].name;
                }
                else if (stepList[i].name != stepList[i - 2].name)
                    stepList.RemoveAt(i--);
                else
                    for (int a = i - 1; a >= 0; a--)
                        if (stepList[a].x == stepList[i].x && stepList[a].y == stepList[i].y)
                            stepList.RemoveAt(i--);
            }

            stepString = "";
            for (int i = 0; i < stepList.Count; i++)
            {
                stepString = stepString + "\n" + stepList[i].name + " -" + Convert.ToChar(stepList[i].x + 65) + Convert.ToChar(stepList[i].y + 65) + "-";
            }

            if (GlobalStepList != null)
                lock (GlobalStepList)
                    GlobalStepList = stepList;
            else
                GlobalStepList = stepList;
        }

        Topic GetTopic(String url)
        {
            try
            {
                Match m = rxUrl.Match(url);
                if (m.Success)
                {
                    uint[] BsnSna = { Convert.ToUInt32(m.Groups["bsn"].Value), Convert.ToUInt32(m.Groups["sna"].Value) };
                    return bh.GetTopicByTopicID(BsnSna[0], BsnSna[1]);
                }
            }
            catch {}
            return null;
        }

        async void LoadGFileAsync()
        {
            //read the file
            String str="";

            try { str = await HC.GetStringAsync(threadUrl); }
            catch { Warning = "網址錯誤。"; return; };

            //find the content start
            int a = str.IndexOf("DOCS_modelChunk"), b;

            if (a < 0) { Warning = "找不到內文起始。"; return; }

            //find the room
            a = str.IndexOf("roomid\\u003d" + (threadRoomId == "" ? "- -" : threadRoomId), a);

            if (a < 0) { Warning = "找不到房間。"; return; }

            //find the end of the content

            b = str.IndexOf("roomid\\u003d", a+1);

            if (b < 0)
            {
                b = str.IndexOf("},", a);
                if (b < 0) { Warning = "找不到內文結尾。"; return; }
            }

            //get the sub string
            str = str.Substring(a, b - a);
            str = str.Replace("\\n", "\n");

            List<Step> stepList = Extract(str);

            if (stepList == null) { Warning = "讀取錯誤。"; return; }

            Load(stepList);
        }

        void LoadFromBH()
        {
            Topic topi = GetTopic(threadUrl);
            if (topi == null) { Warning = "網址錯誤。"; return; }

            List<Comment> com = FindComment(topi, threadFloor);
            if (com == null) { Warning = "樓層錯誤。"; return; }

            List<Step> stepList = Extract(com, threadRoomId);
            if (stepList == null) { Warning = "房號或讀取錯誤。"; return; }

            Load(stepList);
        }

        void UpdateFromText()
        {
            List<Step> stepList = Extract(admi.Text);
            if (stepList == null) { Warning = "讀取失敗。"; return; }

            Load(stepList);
        }

        void LoadingThread()
        {
            for (;; )
            {
                if (IsLoading)
                {
                    Warning = "reading";
                    if (threadUrl != "")
                    {
                        switch(WhichWebsite(threadUrl))
                        {
                            case "BH":
                                LoadFromBH();
                                break;
                            case "GO":
                                LoadGFileAsync();
                                break;
                            case "?":
                                Warning = "這裡是誰 我是哪裡";
                                break;
                        }
                    }
                    else
                        Warning = "網址為空。";

                    //Code here are going to take a breath fo 1s, so it's not reading.
                    if (Warning == "reading")
                        Warning = "";
                }
                Thread.Sleep(1200);
            }
        }
    }
}