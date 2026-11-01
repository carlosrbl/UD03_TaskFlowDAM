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

        private string _nuevoTitulo = string.Empty;
        public string NuevoTitulo { get => _nuevoTitulo; set { _nuevoTitulo = value; OnPropertyChanged(); } }

        private string _nuevoModulo = "DI";
        public string NuevoModulo { get => _nuevoModulo; set { _nuevoModulo = value; OnPropertyChanged(); } }

        private DateTime? _nuevaFechaLimite = DateTime.Now;
        public DateTime? NuevaFechaLimite { get => _nuevaFechaLimite; set { _nuevaFechaLimite = value; OnPropertyChanged(); } }

        private bool _nuevoUrgente;
        public bool NuevoUrgente { get => _nuevoUrgente; set { _nuevoUrgente = value; OnPropertyChanged(); } }

        // Propiedades reactivas para métricas y estado

        private double _progresoPorcentaje;
        public double ProgresoPorcentaje { get => _progresoPorcentaje; set { _progresoPorcentaje = value; OnPropertyChanged(); } }

        private string _textoProgreso = "0 de 0 completadas (0%)";
        public string TextoProgreso { get => _textoProgreso; set { _textoProgreso = value; OnPropertyChanged(); } }

        private int _tareasPendientesCount;
        public int TareasPendientesCount { get => _tareasPendientesCount; set { _tareasPendientesCount = value; OnPropertyChanged(); } }

        private string _estadoApp = "Listo para operar";
        public string EstadoApp { get => _estadoApp; set { _estadoApp = value; OnPropertyChanged(); } }

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
                tarea.PropertyChanged += Tarea_PropertyChanged;
                Tareas.Add(tarea);
            }
            ActualizarMetricas();
        }

        private void ActualizarMetricas()
        {
            if (Tareas.Count == 0)
            {
                ProgresoPorcentaje = 0;
                TextoProgreso = "0 de 0 completadas (0%)";
                TareasPendientesCount = 0;
                return;
            }

            int total = Tareas.Count;
            int completadas = Tareas.Count(t => t.Completada);

            ProgresoPorcentaje = (double)completadas / total * 100;
            TextoProgreso = $"{completadas} de {total} completadas ({Math.Round(ProgresoPorcentaje)}%)";
            TareasPendientesCount = total - completadas;
        }

        private void Tareas_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ActualizarMetricas();
        }

        private void Tarea_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tarea.Completada))
            {
                _context.SaveChanges();
                ActualizarMetricas();
            }
        }

        /// <summary>
        /// Añade una nueva tarea a la base de datos SQLite y a la colección reactiva.
        /// Activado por clic o pulsando la tecla Enter gracias a IsDefault="True".
        /// </summary>
        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NuevoTitulo))
            {
                MessageBox.Show("Introduzca un título para la tarea.", "Campo Obligatorio",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string prioridadSeleccionada = Prioridad.Media;
            if (RbBaja.IsChecked == true) prioridadSeleccionada = Prioridad.Baja;
            if (RbAlta.IsChecked == true) prioridadSeleccionada = Prioridad.Alta;

            var nuevaTarea = new Tarea
            {
                Titulo = NuevoTitulo,
                Modulo = NuevoModulo,
                FechaLimite = NuevaFechaLimite,
                Prioridad = prioridadSeleccionada, 
                EsUrgente = NuevoUrgente,
                Completada = false
            };

            _context.Tareas.Add(nuevaTarea);
            _context.SaveChanges();

            nuevaTarea.PropertyChanged += Tarea_PropertyChanged;
            Tareas.Insert(0, nuevaTarea);

            EstadoApp = "Tarea agregada con éxito";

            BtnLimpiar_Click(sender, e);
        }

        /// <summary>
        /// Restablece los campos del formulario de entrada.
        /// Activado por clic o pulsando la tecla Escape gracias a IsCancel="True".
        /// </summary>
        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            NuevoTitulo = string.Empty;
            NuevoModulo = "DI";
            NuevaFechaLimite = DateTime.Now;
            NuevoUrgente = false;

            RbMedia.IsChecked = true; 
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
                    EstadoApp = "Tarea eliminada";
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