using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Painting_GeneticAlgorithm
{
    class Program
    {
        [DllImport("kernel32.dll", EntryPoint = "GetConsoleWindow", SetLastError = true)]
        private static extern IntPtr GetConsoleHandle();
        static IntPtr handler = GetConsoleHandle();
        
        static void Main(string[] args)
        {
            if (!System.IO.Directory.Exists("datas"))
                System.IO.Directory.CreateDirectory("datas");

            PaintingAlgorithm PA = new PaintingAlgorithm(new Bitmap(Painting_GeneticAlgorithm.Properties.Resources.original_small), 20, 1000);

            for (int gen = 1; ; gen++)
            {
                PA.NextGener();
                using (var graphics = Graphics.FromHwnd(handler))
                using (var image = (Image)(new Bitmap(PA.BestPaint)))
                    graphics.DrawImage(image, 0, 50, PA.BestPaint.Width, PA.BestPaint.Height);
                
                Console.Write("\r" + gen + "세대 " + PA.BestScore + "%                   ");
                PA.BestPaint.Save("datas\\" + gen + "_" + PA.BestScore + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }

    public struct PDNA
    {
        public int x;
        public int y; //위치
        public int d1; //지름1 width
        public int d2; //지름2 height
        public int r, g, b; //RGB
    }

    class PaintingAlgorithm
    {
        //Genetic Algorithm 으로 그림 그리기
        public Bitmap originalPaint; //따라 그릴 그림
        public Bitmap BestPaint; //최고의 그림
        public float BestScore;
        public PDNA[,] dna; //[DNA index, each circle's index] = (x, y, d1, d2, rgb)
        public float[] acc; //정확도
        public int Ndna; //DNA개수
        public int n; //원 개수
        public int max_n; //최대 원 개수

        Random rnd = new Random(DateTime.Now.Millisecond);

        public PaintingAlgorithm(Bitmap original, int numberofDNAs, int numberofCircles)
        {
            originalPaint = new Bitmap(original);
            Ndna = numberofDNAs;
            max_n = numberofCircles; //원의 개수
            n = 1;
            dna = new PDNA[Ndna, max_n];
            acc = new float[Ndna];
            AddRnd();
            CalAccuracy();

            //최고 정확도 찾기
            int maxI = 0;
            for (int j = 0; j < Ndna; j++)
            {
                if (acc[maxI] < acc[j])
                {
                    maxI = j;
                }
            }
            BestPaint = new Bitmap(originalPaint.Width, originalPaint.Height);
            Graphics g = Graphics.FromImage(BestPaint);
            g.Clear(Color.White); // 배경 설정.

            for (int q = 0; q < n; q++)
            {
                SolidBrush sb = new SolidBrush(Color.FromArgb(255 / 2, dna[maxI, q].r, dna[maxI, q].g, dna[maxI, q].b));

                g.FillEllipse(sb, dna[maxI, q].x, dna[maxI, q].y, dna[maxI, q].d1, dna[maxI, q].d2);
            }
        }
        
        public void AddRnd() //새로운 DNA 랜덤 설정
        {
            for (int j = 0; j < Ndna; j++)
            {
                dna[j, n - 1].d1 = rnd.Next(1, 100);
                dna[j, n - 1].d2 = rnd.Next(1, 100);

                dna[j, n - 1].x = rnd.Next(-dna[j, n - 1].d1 + 1, originalPaint.Width + 1);
                dna[j, n - 1].y = rnd.Next(-dna[j, n - 1].d2 + 1, originalPaint.Height + 1);
                
                dna[j, n - 1].r = rnd.Next(0, 256);
                dna[j, n - 1].g = rnd.Next(0, 256);
                dna[j, n - 1].b = rnd.Next(0, 256);
            }
        }

        public void CalAccuracy() //각 DNA의 정확도 계산.
        {
            for (int j = 0; j < Ndna; j++)
            {
                Bitmap tmp = new Bitmap(originalPaint.Width, originalPaint.Height);
                Graphics g = Graphics.FromImage(tmp);
                g.Clear(Color.White); // 배경 설정.

                for (int q = 0; q < n; q++)
                {
                    SolidBrush sb = new SolidBrush(Color.FromArgb(255 / 2, dna[j, q].r, dna[j, q].g, dna[j, q].b));

                    g.FillEllipse(sb, dna[j, q].x, dna[j, q].y, dna[j, q].d1, dna[j, q].d2);
                }

                float total = 0;
                for (int y = 0; y < originalPaint.Height; y++)
                {
                    for (int x = 0; x < originalPaint.Width; x++)
                    {
                        int dR = originalPaint.GetPixel(x, y).R - tmp.GetPixel(x, y).R;
                        int dG = originalPaint.GetPixel(x, y).G - tmp.GetPixel(x, y).G;
                        int dB = originalPaint.GetPixel(x, y).B - tmp.GetPixel(x, y).B;
                        total += (float)Math.Sqrt(dR * dR + dG * dG + dB * dB); //441.6~
                    }
                }
                acc[j] = 100 * total / (float)(originalPaint.Width * originalPaint.Height) / 442;
                acc[j] = 100 - acc[j];
            }
        }

        public void NextGener()
        {
            int[] accIndexer = new int[Ndna]; //정확도 순으로 정렬
            bool[] chk = new bool[Ndna];
            for (int j = 0; j < Ndna; j++)
                chk[j] = false;
            
            for (int j = 0; j < Ndna; j++)
            {
                float b_v = 0; int b_i = 0;
                for (int u = 0; u < Ndna; u++)
                {
                    if (!chk[u] && b_v <= acc[u])
                    {
                        b_v = acc[u];
                        b_i = u;
                    }
                }
                accIndexer[j] = b_i;
                chk[b_i] = true;
            }


            //DNA 세대 교환 시작
            for (int j = 0; j < Ndna / 2; j++)
            {
                PDNA[] newDNA = new PDNA[n];
                Crossover(ref newDNA, accIndexer[j], accIndexer[j + 1]);

                for (int i = 0; i < n; i++)
                {
                    dna[accIndexer[Ndna - j - 1], i] = newDNA[i];
                }
            }

            CalAccuracy(); //정확도 계산

            //최고 정확도 찾기
            int maxI = 0;
            for (int j = 0; j < Ndna; j++)
            {
                if (acc[maxI] < acc[j])
                {
                    maxI = j;
                }
            }
            BestScore = acc[maxI];
            BestPaint = new Bitmap(originalPaint.Width, originalPaint.Height);
            Graphics g = Graphics.FromImage(BestPaint);
            g.Clear(Color.White); // 배경 설정.

            for (int q = 0; q < n; q++)
            {
                SolidBrush sb = new SolidBrush(Color.FromArgb(255 / 2, dna[maxI, q].r, dna[maxI, q].g, dna[maxI, q].b));

                g.FillEllipse(sb, dna[maxI, q].x, dna[maxI, q].y, dna[maxI, q].d1, dna[maxI, q].d2);
            }

            if (n < max_n)
            {
                n++;
                AddRnd();
            }
        }

        public void Crossover(ref PDNA[] tmp, int idx1, int idx2)
        {
            for (int j = 0; j < n; j++)
            {
                if (rnd.Next(1, 101) >= 95)
                {
                    tmp[j].d1 = rnd.Next(1, 100);
                    tmp[j].d2 = rnd.Next(1, 100);

                    tmp[j].x = rnd.Next(-tmp[j].d1 + 1, originalPaint.Width + 1);
                    tmp[j].y = rnd.Next(-tmp[j].d2 + 1, originalPaint.Height + 1);
                    
                    tmp[j].r = rnd.Next(0, 256);
                    tmp[j].g = rnd.Next(0, 256);
                    tmp[j].b = rnd.Next(0, 256);
                }
                else
                {
                    float r = (float)rnd.NextDouble() * (acc[idx1] + acc[idx2]);

                    if (r <= acc[idx1])
                    {
                        tmp[j] = dna[idx1, j];
                    }
                    else
                    {
                        tmp[j] = dna[idx2, j];
                    }
                }
            }
        }

    }
}
