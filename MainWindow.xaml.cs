using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using UD03_TaskFlowDAM.Data;
using UD03_TaskFlowDAM.Models;

namespace UD03_TaskFlowDAM
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml.
    /// Implementa Data Binding directo mediante DataContext y gestiona la persistencia
    /// automática en SQLite utilizando Entity Framework Core (TaskFlowDbContext).
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly TaskFlowDbContext _context;

        // Propiedades enlazadas del formulario de inserción

        // Propiedades reactivas para métricas y estado
 
        public ObservableCollection<Tarea> Tareas { get; } = new();


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _context = new TaskFlowDbContext();
            _context.InicializarBaseDatos();

            CargarTareasDesdeSqlite();
            Tareas.CollectionChanged += Tareas_CollectionChanged;
        }

        /// <summary>
        /// Recupera las tareas desde SQLite a través de EF Core y las suscribe a la colección observable.
        /// </summary>
        private void CargarTareasDesdeSqlite()
        {
            var listaTareas = _context.Tareas.OrderBy(t => t.Completada).ThenByDescending(t => t.Id).ToList();
            Tareas.Clear();

            foreach (var tarea in listaTareas)
            {
                Tareas.Add(tarea);
            }  
        }

        private void Tareas_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }

        /// <summary>
        /// Añade una nueva tarea a la base de datos SQLite y a la colección reactiva.
        /// Activado por clic o pulsando la tecla Enter gracias a IsDefault="True".
        /// </summary>
        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            //if (string.IsNullOrWhiteSpace(NuevoTitulo))
            //{
            //    MessageBox.Show("Por favor, introduzca un título para la tarea.", "Campo Obligatorio",
            //        MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            var nuevaTarea = new Tarea
            {
                //Titulo = ,
                //Modulo = ,
                //FechaLimite = ,
                //Prioridad = ,
                //EsUrgente = ,
                //Completada = 
            };

            // 1. Guardar en SQLite mediante Entity Framework Core
            _context.Tareas.Add(nuevaTarea);
            _context.SaveChanges();

            // 2. Suscribir y añadir a la ObservableCollection para reflejar en XAML
            Tareas.Insert(0, nuevaTarea);

            // 3. Limpiar formulario y notificar
            BtnLimpiar_Click(sender, e);
        }

        /// <summary>
        /// Restablece los campos del formulario de entrada.
        /// Activado por clic o pulsando la tecla Escape gracias a IsCancel="True".
        /// </summary>
        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
    
        }

        /// <summary>
        /// Elimina una tarea individual de SQLite y del listado enlazado.
        /// </summary>
        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Tarea tarea)
            {
                var resultado = MessageBox.Show(
                    $"¿Desea eliminar la tarea '{tarea.Titulo}' de la base de datos?",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    _context.Tareas.Remove(tarea);
                    _context.SaveChanges();
                    Tareas.Remove(tarea);

                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}