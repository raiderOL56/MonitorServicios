using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using Newtonsoft.Json;

using System.Reflection;

namespace Monitor8
{
    public partial class Monitor8 : ServiceBase
    {
        private static System.Timers.Timer aTimer;

        ServiceController sc = new ServiceController();
        string[] myServiceArray;
        int flag = 0;


        public Monitor8()
        {
            InitializeComponent();

            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("MySource"))
            {
                System.Diagnostics.EventLog.CreateEventSource("MySource", "MyNewLog");
            }
            eventLog1.Source = "MySource";
            eventLog1.Log = "MyNewLog";
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Guardar registros en un log usando Nlog
            // TODO: Ejecutar servicio automáticamente al iniciar el servidor
            // TODO: Ejecutar servicio siempre como administrador


            // Guardar json en un Array
            myServiceArray = obtenerJSON();

            setTimer();

            eventLog1.WriteEntry(DateTime.Now + " | El monitoreo de servicios se ha iniciado");
        }

        protected override void OnStop()
        {
            aTimer.Stop();
            aTimer.Dispose();
        }

        private void setTimer()
        {
            //aTimer = new System.Timers.Timer(300000); // 5 minutos
            aTimer = new System.Timers.Timer(30000); // 20 segundos
            aTimer.Elapsed += monitorService;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        // Función que verifica si el servicio existe, su status y lo inicia en caso de estar detenido
        private void monitorService(Object source, ElapsedEventArgs e)
        {
            for (int i = 0; i < myServiceArray.Length; i++)
            {
                sc.ServiceName = myServiceArray[i];

                // Verificar si existe el servicio, e iniciarlizarlo en caso de estar detenido.
                if (serviceExists(myServiceArray[i])) // Si existe el servicio
                {
                    if (!serviceIsRunning(myServiceArray[i], sc)) // El servicio está detenido
                    {
                        startService(myServiceArray[i], sc); // Iniciar el servicio
                    }
                    else // El servicio está iniciado
                    {
                        eventLog1.WriteEntry(DateTime.Now + " hrs. | El servicio " + myServiceArray[i] + " ya está iniciado.");
                    }
                }
                else // No existe el servicio
                {
                    eventLog1.WriteEntry(DateTime.Now + " hrs. | No se encontró el servicio " + myServiceArray[i]);
                }
            }

            flag++;
            
            // Este if se ejecuta cada 15 minutos
            if (flag == 3)
            {
                for (int i = 0; i < myServiceArray.Length; i++)
                {
                    sc.ServiceName = myServiceArray[i];

                    // Verificar si existe el servicio y mandar notificación en caso de que no exista
                    if (serviceExists(myServiceArray[i])) // Si existe
                    {
                        // Verificar si el servicio está detenido para enviar la notificación
                        if (!serviceIsRunning(myServiceArray[i], sc))
                        {
                            eventLog1.WriteEntry(DateTime.Now + " hrs. | NOTIFICACIÓN: El servicio " + myServiceArray[i] + " no se pudo iniciar");
                        }
                    }
                    else // No existe
                    {
                        eventLog1.WriteEntry(DateTime.Now + " hrs. | NOTIFICACIÓN: El servicio " + myServiceArray[i] + " no se encuentra.");
                    }
                }

                flag = 0;
            }
        }

        // Función para verificar si el servicio existe
        bool serviceExists(string ServiceName)
        {
            return ServiceController.GetServices().Any(serviceController => serviceController.ServiceName.Equals(ServiceName));
        }

        // Función para verificar si el servicio está iniciado o detenido
        bool serviceIsRunning(string ServiceName, ServiceController Service)
        {
            if (Service.Status == ServiceControllerStatus.Running)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Iniciar el servicio
        bool startService(string ServiceName, ServiceController Service)
        {
            try
            {
                eventLog1.WriteEntry(DateTime.Now + " hrs. | El servicio " + ServiceName + " está detenido. Iniciando servicio...");
                Service.Start();
                Service.WaitForStatus(ServiceControllerStatus.Running); // Esperar a que se inicialice el servicio
                eventLog1.WriteEntry(DateTime.Now + " hrs. | El servicio " + ServiceName + " se ha iniciado correctamente.");
                return true;
            }
            catch (InvalidOperationException e)
            {
                eventLog1.WriteEntry(DateTime.Now + " hrs. | No se pudo iniciar el servicio" + ServiceName + ". Error: " + e.Message);
                return false;
            }
        }

        // Detener el servicio
        void stopService(string ServiceName)
        {
            try
            {
                eventLog1.WriteEntry(DateTime.Now + " hrs. | Deteniendo servicio...");
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped); // Esperar a que se detenga el servicio
                eventLog1.WriteEntry(DateTime.Now + " hrs. | El servicio " + ServiceName + " se ha detenido correctamente.");
            }
            catch (InvalidOperationException e)
            {
                eventLog1.WriteEntry(DateTime.Now + " hrs. | No se pudo detener el servicio" + ServiceName + ". Error: " + e.Message);
            }
        }

        // Función que devuelve un Array que contiene los valores del .json
        private string[] obtenerJSON()
        {
            string[] services;
            int i = 0, j = 0;

            var path = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\servicios.json";

            using (StreamReader archivo = File.OpenText(path))
            {
                // Leemos los datos del archivo 'servicios.json' desde el inicio hasta el final 
                string json = archivo.ReadToEnd();

                // Deserializamos el archivo 'servicios.json' (Guardamos los datos del .json en un Array)
                dynamic myJsonData = JsonConvert.DeserializeObject(json);

                // Obtener el número de servicios que contiene el .json
                foreach (var item in myJsonData)
                {
                    j++;
                }
                
                services = new string[j];

                // Recorremos el Array que contiene los datos de 'servicios.json'
                foreach (var item in myJsonData)
                {
                    if (i < services.Length)
                    {
                        // Guardar el nombre del servicio en el Array
                        services[i] = item.Servicio;
                        i++;
                    }
                }
            };
            return services;
        }

        private void eventLog1_EntryWritten(object sender, EntryWrittenEventArgs e)
        {

        }
    }
}
