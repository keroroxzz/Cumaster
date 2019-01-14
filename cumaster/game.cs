using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using static global.funcs;

namespace baha_test
{
    public class GameHandler
    {
        Button[,] buttons;
        Button lastButton;
        readonly MainWindow Mw;

        int[] PlayerScore = { 0, 0 };
        String[] names = new String[2];
        public int[,] board = new int[20, 20];


        readonly int[,,] patterns=new int[8,5,2] {
            {{1, 0}, {1, 1}, {1, 2}, {0, 3}, {2, 3}}, //head, neck, others
            {{3, 1}, {2, 1}, {0, 0}, {0, 2}, {1, 1}},
            {{1, 3}, {1, 2}, {0, 0}, {2, 0}, {1, 1}},
            {{0, 1}, {1, 1}, {2, 1}, {3, 0}, {3, 2}},
            {{3, 0}, {2, 1}, {0, 2}, {1, 2}, {1, 3}},
            {{3, 3}, {2, 2}, {0, 1}, {1, 0}, {1, 1}},
            {{0, 3}, {1, 2}, {2, 1}, {3, 1}, {2, 0}},
            {{0, 0}, {1, 1}, {2, 2}, {3, 2}, {2, 3}}};


        public GameHandler(MainWindow Mwind,Button[,] bts)
        {
            buttons = bts;
            Mw = Mwind;
            Clear();
        }

        public void Update(List<Step> steps, Button previewButt, int stepOffset)
        {
            UpdateDisplay(previewButt);
            LoadNames(steps);
            Clear();
            ResetScore();
            LoadFromSteps(steps, stepOffset);
        }

        public void ResetScore()
        {
            PlayerScore[0] = 0;
            PlayerScore[1] = 0;
        }

        public void Clear()
        {
            for (int x = 0; x < board.GetLength(0); x++)
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    board[x, y] = -1;
                }
        }

        public void LoadNames(List<Step> stepList)
        {
            for (int i = 0; i < 2; i++)
                names[i] = (i < stepList.Count) ? stepList[i].name : "";
        }

        public void LoadFromSteps(List<Step> stepList,int offset)
        {
            for (int i = 0; i < stepList.Count + offset; i++) 
            {
                if (InputAndCheck(stepList[i]))
                {
                    lastButton = buttons[stepList[i].x, stepList[i].y];
                    break;
                }
                else if(i== stepList.Count + offset-1)
                    lastButton = buttons[stepList[i].x, stepList[i].y];
            }
        }

        public Boolean IsEmpty(int x,int y)
        {
            return board[x, y] == -1;
        }

        public Boolean InputAndCheck(Step s)
        {
            if (IsEmpty(s.x, s.y))
            {
                int score = -1;

                board[s.x, s.y] = s.id;

                score = CheckPatteren(s.x, s.y);
                if (score>=0)
                {
                    PlayerScore[s.id] += score;
                    return true;
                }
            }
            return false;
        }

        public void UpdateDisplay(Button previewButton)
        {
            Mw.nameboardA.Text = names[0] + "\n" + PlayerScore[0];
            Mw.nameboardB.Text = names[1] + "\n" + PlayerScore[1];

            for (int x = 0; x < buttons.GetLength(0); x++)
            {
                for (int y = 0; y < buttons.GetLength(1); y++)
                {
                    //setting the opacity of buttons
                    if (buttons[x, y] != previewButton)
                    {
                        if (board[x, y] != -1)
                            buttons[x, y].Opacity = 1.0;
                        else
                            buttons[x, y].Opacity = 0.0;
                    }

                    //setting the color of buttons
                    if (board[x, y] == -1)
                        buttons[x, y].Background = Brushes.Gray;
                    else if (board[x, y] == 0)
                        buttons[x, y].Background = Brushes.White;
                    else if (board[x, y] == 1)
                        buttons[x, y].Background = Brushes.Black;

                    buttons[x, y].BorderBrush = null;
                }
            }
            if (lastButton != null)
                lastButton.BorderBrush = Brushes.Red;
        }

        Boolean CheckOnePattern(int x,int y,int n,int c)
        {
            //offset
            x -= patterns[n, c, 0];
            y -= patterns[n, c, 1];

            for (int i = 1; i < patterns.GetLength(1); i++)
            {
                if (!IsInRange(x + patterns[n, i - 1, 0], 0, 19) ||
                    !IsInRange(y + patterns[n, i - 1, 1], 0, 19) ||
                    !IsInRange(x + patterns[n, i, 0], 0, 19) ||
                    !IsInRange(y + patterns[n, i, 1], 0, 19))
                    return false;

                if (board[x + patterns[n, i - 1, 0], y + patterns[n, i - 1, 1]]
                    != board[x + patterns[n, i, 0], y + patterns[n, i, 1]])
                    return false;
            }

            //show the dick
            for (int i = 0; i < patterns.GetLength(1); i++)
            {
                buttons[x + patterns[n, i, 0], y + patterns[n, i, 1]].BorderBrush = Brushes.SeaGreen;
            }
            return true;
        }

        public int CheckPatteren(int x,int y)
        {
            int score = -1; //-1 means no dick

            //check every dick-patterns
            for (int n = 0; n < patterns.GetLength(0); n++)
            {
                //check a pattern with point c as center
                for (int c = 0; c < patterns.GetLength(1); c++)
                {
                    if (CheckOnePattern(x, y, n, c))
                    {
                        if (score == -1)
                            score = 0;

                        score += CheckScore(x, y, n, c);
                    }
                }
            }
            return score;
        }

        public int CheckScore(int x,int y,int n,int c)
        {
            int
                //position of the head
                headX = x + patterns[n, 0, 0] - patterns[n, c, 0],
                headY = y + patterns[n, 0, 1] - patterns[n, c, 1],

                //direction of the dick
                vx = patterns[n, 0, 0] - patterns[n, 1, 0],
                vy = patterns[n, 0, 1] - patterns[n, 1, 1],

                //check-point
                ix = headX + vx,
                iy = headY + vy,

                 scoreOut=0;

            while (true)
            {
                if (!IsInRange(ix, 0, 19) || !IsInRange(iy, 0, 19))
                    break;

                //checking the check-point
                if (board[ix, iy] != -1 && board[headX, headY] != board[ix, iy])
                {
                    scoreOut++;

                    buttons[ix, iy].Background = Brushes.CadetBlue;

                    //moving to the next check-point
                    ix += vx;
                    iy += vy;
                }
                else break;
            }
            return scoreOut;
        }
    }
}
