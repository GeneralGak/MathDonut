
namespace MathDonut
{
	internal class Donut
	{
		private float thetaSpacing = 0.07f;
		private float phiSpacing = 0.02f;

		private float torusCircleRadius = 1;
		private float torusCenterRadius = 2;
		private float constantZDistance = 10;

		private float constantZDistance2;


		public Donut(int _width, int _height)
		{
			// Calculate constantZDistance2 based on screen size: the maximum x-distance occurs
			// roughly at the edge of the torus, which is at x=R1+R2, z=0.  we
			// want that to be displaced 3/8ths of the width of the screen, which
			// is 3/4th of the way from the center to the side of the screen.
			// screen_width*3/8 = constantZDistance2*(R1+R2)/(constantZDistance+0)
			// screen_width*constantZDistance*3/(8*(R1+R2)) = constantZDistance2

			//      Denne projektions formel er fra Donut math: how donut.c works
			constantZDistance2 = _width * constantZDistance * 3 / (8 * (torusCircleRadius + torusCenterRadius));
		}

		public char[,] RenderTorus(float xRotation, float zRotation, int _width, int _height)
		{
			double cosX = Math.Cos(xRotation);
			double sinX = Math.Sin(xRotation);
			double cosZ = Math.Cos(zRotation);
			double sinZ = Math.Sin(zRotation);

			char[,] output = new char[_height, _width];
			double[,] zbuffer = new double[_height, _width];

			// sætter værdiger i arrays
			for (int i = 0; i < _height; i++)
			{
				for (int j = 0; j < _width; j++)
				{
					output[i, j] = ' ';
					zbuffer[i, j] = 0;
				}
			}

			// Efter at have fundet de to radiuser på torus, så bruger vi theta
			// til at bestemme vinklen på den ydre cirkel, hvor det næste punkt skal sættes

			//      Brugen af de to for-loops med theta og phi er taget fra Donut math: how donut.c works
			for (float theta = 0; theta < 2 * Math.PI; theta += thetaSpacing)
			{
				// finder cos og sin af theta
				double cosTheta = Math.Cos(theta);
				double sinTheta = Math.Sin(theta);

				// Efter at punktets position er blevet sat, bruger vi Phi
				// til at rotere punktet omkring Y aksen, så torus får en 3D form
				for (float phi = 0; phi < 2 * Math.PI; phi += phiSpacing)
				{
					// finder cos og sin af phi
					double cosphi = Math.Cos(phi);
					double sinphi = Math.Sin(phi);

					// Sætter positionen på det næste punkt i torusen, før den bliver roteret
					//                                                                      X                                       Y               Z
					double[,] circlePosition = new double[,] { { torusCenterRadius + torusCircleRadius * cosTheta, torusCircleRadius * sinTheta, 0 } };

					// Rotations matrix til at dreje punktet rundt om y-aksen
					double[,] circleRotation = { { cosphi, 0,-sinphi },
												 {    0,   1,  0     },
												 { sinphi, 0, cosphi } };

					// Som vi ganger sammen med positions vectoren

					// Samtidig skal den samlede torus kunne dreje rundt om sit center punkt
					// For at gøre det, ganger vi to andre rotations matricer på

					double[,] rotateAroundX = { {   1,      0,    0  },
												{   0,    cosX, sinX },
												{   0,   -sinX, cosX } };

					double[,] rotateAroundZ = { { cosZ, sinZ,     0 },
												{-sinZ, cosZ,     0 },
												{   0,    0,      1 } };

					double[,] rotatedCirclePosition = MultiplyMatrices(circlePosition, MultiplyMatrices(circleRotation, MultiplyMatrices(rotateAroundX, rotateAroundZ)));

					// final 3D (x,y,z) coordinate after rotations, directly from
					// our math above

					//      Denne formel er fra Donut math: how donut.c works
					//double x = circlePosition[0, 0] * (cosZ * cosphi + sinX * sinZ * sinphi) - circlePosition[0, 1] * cosX * sinZ;
					//double y = circlePosition[0, 0] * (sinZ * cosphi - sinX * cosZ * sinphi) + circlePosition[0, 1] * cosX * cosZ;
					//double z = constantZDistance + cosX * circlePosition[0, 0] * sinphi + circlePosition[0, 1] * sinX;
					double ooz = 1 / (constantZDistance + rotatedCirclePosition[0, 2]);  // "one over z"

					// Vi laver herefter en projektion af X og Y koordinaterne,
					// så koordinaterne går fra at være 3D til en 2D position på skærmen.
					// Ved projektionen bliver y sat negativt, fordi y koordinaten går ned på 2D skærmen

					//      Denne projektions formel er fra Donut math: how donut.c works
					int xp = (int)((_width / 2) + constantZDistance2 * ooz * rotatedCirclePosition[0, 0]);
					int yp = (int)((_height / 2) - constantZDistance2 * ooz * rotatedCirclePosition[0, 1]);


					// Efter at have sat cirkl punktets position og rotation kigger vi nu efter 
					// at kalkulere belysningen på torus overflade

					//      Mange elementer af koden der står for belysning er også lånt fra Donut math: how donut.c works

					// Først bestemmer vi normalerne på hvert cirkelpunkt
					double[,] surfaceNormal = MultiplyMatrices(new double[,] { { cosTheta, sinTheta, 0 } }, MultiplyMatrices(circleRotation, MultiplyMatrices(rotateAroundX, rotateAroundZ)));

					// En vektor brugt til at bestemme lysets retning
					double[,] lightDirection = new double[,] { { 0, -1, 1 } };

					// Til sidst udregner vi hvor "belyst" punktet skal være
					double luminance = (surfaceNormal[0, 0] * lightDirection[0, 0]) - (surfaceNormal[0, 1] * lightDirection[0, 1]) - (surfaceNormal[0, 2] * lightDirection[0, 2]);

					//double luminance2 = cosphi * cosTheta * sinZ - cosX * cosTheta * sinphi - sinX * sinTheta + cosZ * 
					//                   (cosX * sinTheta - cosTheta * sinX * sinphi);

					// L ranges from -sqrt(2) to +sqrt(2).  If it's < 0, the surface
					// is pointing away from us, so we won't bother trying to plot it.
					if (luminance > 0)
					{
						// test against the z-buffer.  larger 1/z means the pixel is
						// closer to the viewer than what's already plotted.
						if (ooz > zbuffer[yp, xp])
						{
							zbuffer[yp, xp] = ooz;
							int luminance_index = (int)(luminance * 8);
							// luminance_index is now in the range 0..11 (8*sqrt(2) = 11.3)
							// now we lookup the character corresponding to the
							// luminance and plot it in our output:
							output[yp, xp] = ".,-~:;=!*#$@"[luminance_index];
						}
					}
				}
			}

			return output;
		}

		static double[,] MultiplyMatrices(double[,] matrix1, double[,] matrix2)
		{
			// Find længde og højde på de to matricer
			int matrix1Row = matrix1.GetLength(0);
			int matrix1Column = matrix1.GetLength(1);
			int matrix2Row = matrix2.GetLength(0);
			int matrix2Column = matrix2.GetLength(1);
			double[,] result = new double[matrix1Row, matrix2Column];

			// Antal søjler i første matrix skal matche antal rækker i den anden matrix
			// før man kan multiplicere dem
			if (matrix1Column != matrix2Row)
			{
				Console.WriteLine("Matrixes can't be multiplied!!");

				return result;
			}

			// Vi gør her brug af række-søjle reglen
			// Hvor vi tager et element fra rækken på den første Matrix og ganger sammen med et element fra søjlen på den anden Matrix 
			// resultatet bliver herefter lagt sammen med resultater fra at gange efterlølgene elementer
			// Det endelige resultat heder prikproduktet og bliver tilføjet til en ny matrix
			for (int i = 0; i < matrix1Row; i++)
			{
				for (int j = 0; j < matrix2Column; j++)
				{
					result[i, j] = 0;
					for (int k = 0; k < matrix1Column; k++)
					{
						result[i, j] += matrix1[i, k] * matrix2[k, j];
					}
				}
			}
			return result;
		}
	}
}
