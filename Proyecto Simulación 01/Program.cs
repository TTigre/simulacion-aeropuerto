using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Proyecto_Simulación_01
{
    class Aeropuerto
    {
        double lambdaCargaYDescarga, lambdaRotura, lambdaCombustible, mediaAterrizaje, desviacionAterrizaje, mediaDespegue, desviacionDespegue, lamdaLlegadas;

        //tiempo de salida del ultimo avion que estuvo en cada pista, inicializado a 0 por defecto
        double[] tiemposDeSalida;

        //Arbol que servira de heap multivalor para hallar el minimo de forma eficiente
        SortedDictionary<double, Queue<int>> tiemposOrdenados = new SortedDictionary<double, Queue<int>>();

        //Tiempo que cada pista se encuentra vacia hasta el momento
        double[] tiempoVacia;
        int indexTop = 0;
        public Aeropuerto(int cantidadDePistas, double lamdaLlegadas=20, double lambdaCargaYDescarga=30, double lambdaRotura=15, 
            double lambdaCombustible=30, double mediaAterrizaje=10, double desviacionAterrizaje=5,
            double mediaDespegue=10, double desviacionDespegue=5)
        {
            this.lambdaCargaYDescarga = lambdaCargaYDescarga;
            this.lambdaRotura = lambdaRotura;
            this.lambdaCombustible = lambdaCombustible;
            this.mediaAterrizaje = mediaAterrizaje;
            this.desviacionAterrizaje = desviacionAterrizaje;
            this.mediaDespegue = mediaDespegue;
            this.desviacionDespegue=desviacionDespegue;
            this.lamdaLlegadas = lamdaLlegadas;

            tiemposDeSalida = new double[cantidadDePistas];
            tiempoVacia = new double[cantidadDePistas];
            tiemposOrdenados.Add(0, new Queue<int>());
            for(int i=0; i<cantidadDePistas;i++)
            {
                tiemposOrdenados[0].Enqueue(i);
            }

        }
        public void Reset()
        {
            tiemposDeSalida = new double[tiemposDeSalida.Length];
            tiempoVacia = new double[tiempoVacia.Length];
            tiemposOrdenados = new SortedDictionary<double, Queue<int>>();
            tiemposOrdenados.Add(0, new Queue<int>());
            for (int i = 0; i < tiemposDeSalida.Length; i++)
            {
                tiemposOrdenados[0].Enqueue(i);
            }
            indexTop = 0;
        }

        //Devuelve true en caso de que en ese momento haya una pista del aeropuerto disponible
        public bool Disponible(double tiempo)
        {
            return tiemposDeSalida[indexTop]<=tiempo;
        }

        //Devuelve el valor de la pista que mas pronto se vacie o lleve mas tiempo vacia
        public double MinDisponible()
        {
            return tiemposDeSalida[indexTop];
        }

        //Agrega un avion a alguna pista
        public void Agrega(double tiempo)
        {
            double tiempotermina = SimulaEstancia() + tiempo;
            int minindex = indexTop;
            tiempoVacia[minindex] += tiempo - tiemposDeSalida[minindex];
            tiemposOrdenados[tiemposDeSalida[minindex]].Dequeue();

            if (tiemposOrdenados[tiemposDeSalida[minindex]].Count == 0)
                tiemposOrdenados.Remove(tiemposDeSalida[minindex]);

            tiemposDeSalida[minindex] = tiempotermina;

            if (!tiemposOrdenados.ContainsKey(tiempotermina))
                tiemposOrdenados[tiempotermina] = new Queue<int>();

            tiemposOrdenados[tiempotermina].Enqueue(minindex);
            indexTop = HallaIndexMin();
        }
        int HallaIndexMinOld()
        {

            double min = tiemposDeSalida[0];
            int minindex = 0;
            for (int i = 1; i < tiemposDeSalida.Length; i++)
            {
                if(tiemposDeSalida[i]<min)
                {
                    min = tiemposDeSalida[i];
                    minindex = i;
                }
            }
            return minindex;
        }

        //Hallar el mínimo sin eliminarlo
        int HallaIndexMin()
        {
            return tiemposOrdenados.First().Value.Peek();
        }
        //Simula el tiempo que se debe demorar un avion en la pista
        double SimulaEstancia()
        {
            double tiempoCargaYDescarga = 0;
            if (RandomFuncs.rand.NextDouble() < 0.5)
                tiempoCargaYDescarga = RandomFuncs.ExponencialVar(lambdaCargaYDescarga);

            double tiempoRotura = 0;
            if ((RandomFuncs.rand.NextDouble() < 0.1))
                tiempoRotura = RandomFuncs.ExponencialVar(lambdaCargaYDescarga);

            //tiempo con la pista ocupada = tiempo de carga y descarga + recarga de combustible + tiempo de despegue + tiempo de aterrizaje + tiempo de arreglo de rotura
            double tiempoTotal = tiempoCargaYDescarga + RandomFuncs.ExponencialVar(lambdaCombustible) + RandomFuncs.NormalVar(mediaAterrizaje, desviacionAterrizaje)+ RandomFuncs.NormalVar(mediaDespegue, desviacionDespegue) + tiempoRotura;

            return tiempoTotal;
        }

        //Realiza la simulacion de los aviones llegando y saliendo por una semana
        public double[] PistasVacias()
        {
            foreach(double llegada in TiempoLLegada(lamdaLlegadas))
            {
                double minimo = MinDisponible();
                if (llegada>60*24*7||minimo> 60 * 24 * 7)
                {
                    while(Disponible(60 * 24 * 7))
                    {
                        Agrega(60 * 24 * 7);
                    }
                    break;
                }
                if(Disponible(llegada))
                {
                    Agrega(llegada);
                }
                else
                {
                    Agrega(minimo);
                }
            }
            return this.tiempoVacia.Clone() as double[];
        }
        static IEnumerable<double> TiempoLLegada(double media)
        {
            double LlegadaAcum = 0;
            while (true)
            {
                double tiempo = RandomFuncs.ExponencialVar(media);
                LlegadaAcum += tiempo;
                yield return LlegadaAcum;
            }
        }
    }
    public class RandomFuncs
    {
        public static Random rand = new Random();
        public static double ExponencialVar(double lambda)
        {
            return -Math.Log(rand.NextDouble()) / lambda;
        }
        public static double NormalVar(double media, double desviacion)
        {
            return Normal01() * desviacion + media;
        }
        public static double Normal01()
        {
            while(true)
            {
                double Y1 = ExponencialVar(1);
                double Y2 = ExponencialVar(1);
                if(Y2-((Y1-1.0)*(Y1 - 1.0))/2.0>=0)
                {
                    if (rand.NextDouble() > 0.5)
                        return -Y1;
                    return Y1;
                }
            }
        }
    }
    class Program
    {
        static double LlegadaAcum = 0;
        static void Main(string[] args)
        {
            Stopwatch crono = new Stopwatch();
            crono.Start();
            for (int e = 0; e < 6; e++)
            {
                Aeropuerto air;
                if(e==0)
                    air = new Aeropuerto(3, 1.0 / 17, 1.0 / 30, 1.0 / 15, 1.0 / 30, 1.0 / 10, 1.0 / 5, 1.0 / 10, 1.0 / 5);
                else if(e==1)
                    air = new Aeropuerto(5, 1.0 / 20, 1.0 / 30, 1.0 / 15, 1.0 / 30, 1.0 / 10, 1.0 / 5, 1.0 / 10, 1.0 / 5);
                else if(e==2)
                    air = new Aeropuerto(2, 1.0 / 20, 1.0 / 30, 1.0 / 15, 1.0 / 30, 1.0 / 10, 1.0 / 5, 1.0 / 10, 1.0 / 5);
                else if (e == 3)
                    air = new Aeropuerto(3, 1.0 / 15, 1.0 / 30, 1.0 / 15, 1.0 / 30, 1.0 / 10, 1.0 / 5, 1.0 / 10, 1.0 / 5);
                else if(e==4)
                    air = new Aeropuerto(3, 1.0 / 16, 1.0 / 30, 1.0 / 15, 1.0 / 30, 1.0 / 10, 1.0 / 5, 1.0 / 10, 1.0 / 5);
                else
                    air = new Aeropuerto(5, 20, 30, 15, 30, 10, 5, 10, 5);
                double total = 0;
                int pistastotales = 0;
                for (int i = 0; i < 1000; i++)
                {
                    //Console.WriteLine("Simulacion " + i + ":");
                    air.Reset();
                    double[] result = air.PistasVacias();
                    //for (int e = 0; e < result.Length; e++)
                    //{
                    //    Console.WriteLine("Minutos que estuvo vacia la pista " + e + "=>" + result[e]);
                    //}
                    pistastotales += result.Length;
                    foreach (double r in result)
                        total += r;
                }
                double promedio = total / pistastotales;
                if (e == 0)
                    Console.WriteLine("El promedio con el valor recomendado=" + promedio);
                else if (e == 1)
                    Console.WriteLine("El promedio con 5 pistas=" + promedio);
                else if (e == 2)
                    Console.WriteLine("El promedio con 2 pistas=" + promedio);
                else if (e == 3)
                    Console.WriteLine("El promedio con 3 pistas y llegadas cada 15 minutos=" + promedio);
                else if (e == 4)
                    Console.WriteLine("El promedio con 3 pistas y llegadas cada 16 minutos=" + promedio);
                else
                    Console.WriteLine("El promedio con los valores originales=" + promedio);
            }
            crono.Stop();
            Console.WriteLine("Se demoro por simulacion " + crono.ElapsedMilliseconds / 1000.0 + " milisegundos");
        }
        
    }
}
