namespace MathDonut
{
    internal class Program
    {
        private static int consoleWidth = Console.BufferWidth;
        private static int consoleHeight = Console.BufferHeight;

        static void Main(string[] args)
        {
            Console.SetWindowSize(50, 50);

			Thread donutThreat = new Thread(RenderThread);

			donutThreat.Start();
        }

        public static void RenderThread()
        {
			consoleWidth = Console.WindowWidth;
			consoleHeight = Console.WindowHeight;

			Donut mathDonut = new Donut(consoleWidth, consoleHeight);

			float xRotation = 0;
			float yRotation = 0;

			while (true)
			{
				xRotation += 0.1f;
				yRotation += 0.1f;

				char[,] renderedDonut = mathDonut.RenderTorus(xRotation, yRotation, consoleWidth, consoleHeight);
				string display = "";

				for (int j = 0; j < consoleHeight; j++)
				{
					for (int i = 0; i < consoleWidth; i++)
					{
						display += renderedDonut[j, i];
						//Console.Write(renderedDonut[i, j]);
					}

					display += '\n';
					//Console.Write('\n');
				}

				Console.Clear();
				Console.WriteLine(display);

				Thread.Sleep(60);
			}
		}
    }
}
