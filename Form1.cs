using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;

namespace Projeto_Gerador_de_Código_Gcode
{
    public partial class Form1 : Form
    {


        public Form1()
        {
            InitializeComponent();
        }

        Image<Gray, Byte> image_general;
        double[,] Matriz_imagem;
        int linhas; int colunas;
        bool evento = false;
        double z_seguro = 0;

        public Bitmap processImage(Bitmap image)
        {
            Bitmap returnMap = new Bitmap(image.Width, image.Height,
                                   PixelFormat.Format32bppArgb);
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0,
                                     image.Width, image.Height),
                                     ImageLockMode.ReadOnly,
                                     PixelFormat.Format32bppArgb);
            BitmapData bitmapData2 = returnMap.LockBits(new Rectangle(0, 0,
                                     returnMap.Width, returnMap.Height),
                                     ImageLockMode.ReadOnly,
                                     PixelFormat.Format32bppArgb);
            int a = 0;

            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;
                byte* imagePointer2 = (byte*)bitmapData2.Scan0;
                for (int i = 0; i < bitmapData1.Height; i++)
                {
                    for (int j = 0; j < bitmapData1.Width; j++)
                    {
                        // write the logic implementation here
                        a = (imagePointer1[0] + imagePointer1[1] +
                             imagePointer1[2]);
                        // richTextBox1.Text = string.Concat(richTextBox1.Text, a.ToString());
                        // richTextBox1.Text = string.Concat(richTextBox1.Text, " ");
                        imagePointer2[0] = (byte)a;
                        imagePointer2[1] = (byte)a;
                        imagePointer2[2] = (byte)a;
                        imagePointer2[3] = imagePointer1[3];
                        //4 bytes per pixel
                        imagePointer1 += 4;
                        imagePointer2 += 4;
                    }//end for j
                    //4 bytes per pixel
                    imagePointer1 += bitmapData1.Stride -
                                    (bitmapData1.Width * 4);
                    imagePointer2 += bitmapData1.Stride -
                                    (bitmapData1.Width * 4);
                }//end for i
            }//end unsafe
            returnMap.UnlockBits(bitmapData2);
            image.UnlockBits(bitmapData1);
            return returnMap;
        }//end processImage

        private void button1_Click(object sender, EventArgs e)
        {
            int size = -1;
            Bitmap image1;
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {

                string file = openFileDialog1.FileName;
                try
                {
                    string text = File.ReadAllText(file);
                    size = text.Length;
                    textBox1.Text = file;
                    //pictureBox1.ImageLocation = file;

                    image1 = new Bitmap(@file, true);

                   

                    // Display the pixel format in Label1.
                    
                    textBox4.Text = image1.Size.Height.ToString();
                    textBox3.Text = image1.Size.Width.ToString();
                    image1.Dispose();


                }
                catch (IOException)
                {
                }
            }
          
        }

        private List<Tuple<int, int>> mascara(double raio, int tamanhox, int tamanhoy)
        {

            var lista_de_pontos = new List<Tuple<int, int>>();

            //Algoritmo de Bresenham para circunferencia
            // f(x,y) = x² + y² - R²
            // f > 0 = fora do circulo
            // f <= 0 dentro do circulo


            for (int i = - Convert.ToInt32(raio); i <= Convert.ToInt32(raio); i = i + tamanhox)
            {
                for (int j = - Convert.ToInt32(raio); j <= Convert.ToInt32(raio); j = j + tamanhoy)
                {

                    if ((i * i) + (j * j) - (raio * raio) <= 0)
                    {
                        //Está dentro
                        lista_de_pontos.Add(Tuple.Create(i, j));
                    }
                }
            }

            return lista_de_pontos;
        }


        private double marca_pixel(int x, int y, List<Tuple<int, int>> mascara, Image<Gray, Byte> img)
        {

            double value_temp = 0;
           
            int temp_x = 0;
            int temp_y = 0;

            try
            {

                double value = img[x, y].Intensity;
                //Parallel.ForEach(mascara, i => 
               
                foreach (var i in mascara)
                {

                    temp_x = x + i.Item1;
                    temp_y = y + i.Item2;
                    try
                    {

                        value_temp = img[temp_x, temp_y].Intensity;
                    }
                    catch
                    {
                        return 500;
                    }

                    if (value_temp < value)
                    {
                        value = value_temp;
                    }

                };

                return value;


            }
            catch
            {

                return 500;
            }

        }
        
        private bool detector_colisao(double[,] Matriz_imagem, int linhas, int colunas, double diametro_fresa, int pixelpormm, double profundidade, double corte)
        {


            var lista_de_pontos = mascara((diametro_fresa / 2), pixelpormm, pixelpormm);
            var lista_de_pontos2 = mascara((diametro_fresa / 2) + 1, pixelpormm, pixelpormm);

            var lista_resultante = lista_de_pontos2.Except(lista_de_pontos);

            
            /*
            
            foreach (var i in lista_resultante)
            {

                
            }


            */
          



            for (int i = 0; i < linhas; i++)
            {
                for (int j = 0; j < colunas; j++)
                {

                    foreach (var k in lista_resultante)
                    {
                        try
                        {

                            if (Matriz_imagem[i, j] != 500 && Matriz_imagem[i + k.Item1, j + k.Item2] != 500)
                            {
                                double valor = Matriz_imagem[i, j] - Matriz_imagem[i + k.Item1, j + k.Item2];
                                if (valor < 0)
                                {
                                    var passo_normalizado = (Math.Abs(valor) / 255) * profundidade;
                                    if (passo_normalizado > corte)
                                    {

                                        MessageBox.Show("colisao em x:" + i.ToString() + " y:" + j.ToString());
                                    }
                                }
                            }

                            
                        }
                        catch
                        {

                        }

                    }



                }
            }

        

            return true;


        }


        private void gerador_gcode(double[,] Matriz_imagem, string nome_arquivo)
        {
            double valor = 0;
            //int tamanho_pixel_x = img.Width;
            //int tamanho_pixel_y = img.Height;


            double diametro_fresa = Double.Parse(textBox2.Text);
            double raio = diametro_fresa / 2;


            //textBox4.Text = img.Size.Height.ToString();
            //textBox3.Text = img.Size.Width.ToString();


            
            z_seguro = 2;
            bool inicial = true;
            double altura_topo;
            double altura_base;
            double altura = 0;
            double profundidade = Double.Parse(textBox5.Text);
            double velocidade = 2000; // mm/s

            StreamWriter writer = new StreamWriter(nome_arquivo + ".txt");

            writer.Write("T1M6\r\n");
            writer.Write("G0Z" + z_seguro.ToString() + ".000" + "\r\n");
            writer.Write("G0X0.000Y0.000S15000M3\r\n");

            raio = diametro_fresa / 2;

            for (int i = 0; i < linhas; i++)
            {

                if (i % 2 == 0)
                {
                    for (int j = 0; j < colunas; j++)
                    {

                        if (j % diametro_fresa == 0)
                        {

                            if (Matriz_imagem[i, j] != 500)
                            {

                                if (inicial == true)
                                {
                                    
                                    altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                    
                                    writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                    writer.Write("G1" + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "F" + velocidade.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                    inicial = false;

                                }



                                else
                                {
                                    altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                    writer.Write("G1X" + j + "Y" + i + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                }

                            }
                            else
                            {

                                writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                            }

                        }
                        else
                        {
                           //if (j == raio)
                            //{

                                if (Matriz_imagem[i, j] != 500)
                                {

                                    if (inicial == true)
                                    {

                                        altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                        writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                        writer.Write("G1" + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "F" + velocidade.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                        inicial = false;

                                    }



                                    else
                                    {
                                        altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                        writer.Write("G1X" + j + "Y" + i + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                    }

                                }
                                else
                                {


                                    writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                }


                            }


                      // }


                    }


                }

                else
                {
                    for (int j = colunas - 1; j >= 0; j--)
                    {

                        if (j % diametro_fresa == 0)
                        {


                            if (Matriz_imagem[i, j] != 500)
                            {

                                if (inicial == true)
                                {
                                    altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                    writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                    writer.Write("G1" + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "F" + velocidade.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                    inicial = false;


                                }

                                else
                                {
                                    altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                    writer.Write("G1X" + j + "Y" + i + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                }
                            }
                            else
                            {

                                writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                            }
                            //writer.Write("(" + i.ToString() + "," + j.ToString() + "," + Matriz_imagem[i, j].ToString() + ") ");
                        }
                        else
                        {
                            //if (j == raio)
                            //{
                                if (Matriz_imagem[i, j] != 500)
                                {

                                    if (inicial == true)
                                    {

                                        altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                        writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                        writer.Write("G1" + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "F" + velocidade.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                        inicial = false;

                                    }



                                    else
                                    {
                                        altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                        writer.Write("G1X" + j + "Y" + i + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                    }

                                }
                                else
                                {
                                    writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                }

                            //}

                        }
                    }

                }

            }



            writer.Write("G0" + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
            writer.Write("G0X0.000Y0.000S15000M3\r\n");
            writer.Write("M30");

            writer.Close();
            MessageBox.Show("Foi");
           
              
            

        }


        //GERA GCODE DE UMA IMAGEM INVERTENDO OS EIXOS X E Y
        private void gerador_gcode_inv(double[,] Matriz_imagem, string nome_arquivo)
        {
            double valor = 0;
            //int tamanho_pixel_x = img.Width;
            //int tamanho_pixel_y = img.Height;


            double diametro_fresa = double.Parse(textBox2.Text);
            double raio = diametro_fresa / 2;


            //textBox4.Text = img.Size.Height.ToString();
            //textBox3.Text = img.Size.Width.ToString();



            z_seguro = 2;
            bool inicial = true;
            double altura_topo;
            double altura_base;
            double altura = 0;
            double profundidade = Double.Parse(textBox5.Text);
            double velocidade = 2000; // mm/s

            StreamWriter writer = new StreamWriter(nome_arquivo + ".txt");

            writer.Write("T1M6\r\n");
            writer.Write("G0Z" + z_seguro.ToString() + ".000" + "\r\n");
            writer.Write("G0X0.000Y0.000S15000M3\r\n");

            raio = diametro_fresa / 2;

            for (int j = 0; j < colunas; j++)
            {

                if (j % 2 == 0)
                {
                    for (int i = 0; i < linhas; i++)
                    {

                        if (i % diametro_fresa == 0)
                        {

                            if (Matriz_imagem[i, j] != 500)
                            {

                                if (inicial == true)
                                {

                                    altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;

                                    writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                    writer.Write("G1" + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "F" + velocidade.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                    inicial = false;

                                }



                                else
                                {
                                    altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                    writer.Write("G1X" + j + "Y" + i + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                }

                            }
                            else
                            {

                                writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                            }

                        }
                        else
                        {
                            //if (j == raio)
                            //{

                                if (Matriz_imagem[i, j] != 500)
                                {

                                    if (inicial == true)
                                    {

                                        altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                        writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                        writer.Write("G1" + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "F" + velocidade.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                        inicial = false;

                                    }



                                    else
                                    {
                                        altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                        writer.Write("G1X" + j + "Y" + i + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                    }

                                }
                                else
                                {


                                    writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                }


                            //}


                        }


                    }


                }

                else
                {
                    for (int i = linhas - 1; i >= 0; i--)
                    {

                        if (i % diametro_fresa == 0)
                        {


                            if (Matriz_imagem[i, j] != 500)
                            {

                                if (inicial == true)
                                {
                                    altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                    writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                    writer.Write("G1" + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "F" + velocidade.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                    inicial = false;


                                }

                                else
                                {
                                    altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                    writer.Write("G1X" + j + "Y" + i + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                }
                            }
                            else
                            {

                                writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                            }
                            //writer.Write("(" + i.ToString() + "," + j.ToString() + "," + Matriz_imagem[i, j].ToString() + ") ");
                        }
                        else
                        {
                            //if (j == raio)
                            //{
                                if (Matriz_imagem[i, j] != 500)
                                {

                                    if (inicial == true)
                                    {

                                        altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                        writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                        writer.Write("G1" + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "F" + velocidade.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
                                        inicial = false;

                                    }



                                    else
                                    {
                                        altura = ((255 - Matriz_imagem[i, j]) / 255) * profundidade;
                                        writer.Write("G1X" + j + "Y" + i + "Z-" + altura.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                    }

                                }
                                else
                                {
                                    writer.Write("G0X" + j + "Y" + i + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");

                                }

                            //}

                        }
                    }

                }

            }



            writer.Write("G0" + "Z" + z_seguro.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\r\n");
            writer.Write("G0X0.000Y0.000S15000M3\r\n");
            writer.Write("M30");

            writer.Close();
            MessageBox.Show("Foi");




        }

        static double[,] ConvertMatrix(double[] flat, int m, int n)
        {
            if (flat.Length != m * n)
            {
                throw new ArgumentException("Invalid length");
            }
            double[,] ret = new double[m, n];
            // BlockCopy uses byte lengths: a double is 8 bytes
            Buffer.BlockCopy(flat, 0, ret, 0, flat.Length * sizeof(double));
            return ret;
        }


        private void button2_Click(object sender, EventArgs e)
        {

            Bitmap image1 = new Bitmap(@textBox1.Text, true);

            image_general = new Image<Gray, Byte>(@textBox1.Text);

            double profundidade = double.Parse(textBox5.Text);

         

            image_general = new Image<Gray, Byte>(@textBox1.Text);
            linhas = image_general.Height;
            colunas = image_general.Width;
            var imagelist = new List<Image<Gray,double>>();
            Image<Gray,double> depthImage = new Image<Gray,double>(colunas, linhas);
            double valor = 0;
            int tamanho_pixel_x = image_general.Width;
            int tamanho_pixel_y = image_general.Height;
            int pixelpormm = 1;


            double diametro_fresa = Double.Parse(textBox2.Text);
            double raio = diametro_fresa / 2;
            double corte = Double.Parse(textBox6.Text);

            var mascara_pontos = mascara(raio, pixelpormm, pixelpormm);
            /*
            foreach (var i in mascara_pontos)
            {
                
                richTextBox1.Text = richTextBox1.Text + i.Item1.ToString() + "," + i.Item2.ToString() + " ";
            }
           */

            
            double passo_max_vertical = 5;
            Matriz_imagem = new double[linhas, colunas];

            double quantidade_img = profundidade / passo_max_vertical;
            double passo_normalizado = 0;

             for (int k = 1; k <= quantidade_img; k++)
             {


                Image<Gray, double> depthImage2 = new Image<Gray, double>(colunas, linhas);
                for (int i = 0; i < linhas; i++)
                {
                    for (int j = 0; j < colunas; j++)
                    {

                        valor = marca_pixel(i, j, mascara_pontos, image_general);

                        passo_normalizado = ((255 - valor) / 255)*profundidade;
                        if (k > 1)
                        {
                            if (passo_normalizado < (k - 1) * passo_max_vertical)
                            {
                                valor = 500;

                            }
                            else
                            {

                                if (passo_normalizado > k * passo_max_vertical)
                                {

                                    valor = (1 - (k * passo_max_vertical / profundidade)) * 255;
                                }
                            }

                        }
                        else
                        {
                            if (passo_normalizado > k * passo_max_vertical)
                            {

                                valor = (1 - (k * passo_max_vertical / profundidade)) * 255;
                            }
                        }
                        Matriz_imagem[i, j] = valor;

                        if (valor == 500)
                        {
                            depthImage2.Data[i, j, 0] = 255;
                        }
                        else
                        {

                            depthImage2.Data[i, j, 0] = valor;
                        }

                    }//end for j

                };//end for i


                    imagelist.Add(depthImage2);
                    
                    gerador_gcode(Matriz_imagem, "Imagem" + k.ToString());
                    
                
              }
             int cont=1;
             foreach (Image<Gray, double> images in imagelist)
             {
                 
                 imageBox1.Image = images;
                 MessageBox.Show("Imagem = " + cont.ToString());
                 cont++;
             }

             for (int i = 0; i < linhas; i++)
             {
                 for (int j = 0; j < colunas; j++)
                 {

                     valor = marca_pixel(i, j, mascara_pontos, image_general);
                     Matriz_imagem[i, j] = valor;
                     depthImage.Data[i, j, 0] = valor;
                     
                     
                 }
             }

             detector_colisao(Matriz_imagem, linhas, colunas, diametro_fresa, 1, profundidade,corte);
             gerador_gcode_inv(Matriz_imagem, "Ultima Imagem");
             imageBox1.Image = depthImage;
             MessageBox.Show("mostra ibagem");
    
        }

      
    }

}