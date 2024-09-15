using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class FATEntry
{
    public string FileName { get; set; } = string.Empty; 
    public string InitialDataFile { get; set; } = string.Empty; 
    public bool IsInRecycleBin { get; set; } = false;
    public int TotalCharacters { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.Now;
    public DateTime ModificationDate { get; set; } = DateTime.Now;
    public DateTime? DeletionDate { get; set; } 
}

public class DataFile
{
    public string Data { get; set; } = string.Empty; 
    public string? NextDataFile { get; set; } 
    public bool Eof { get; set; }
}

class Program
{
    static string directory = "C:\\Users\\david\\OneDrive\\Desktop\\Proyecto_1_manejo_de_archivos";

    static void Main()
    {
        while (true)
        {
            Console.WriteLine("\nMenú de opciones:");
            Console.WriteLine("1. Crear un archivo");
            Console.WriteLine("2. Listar archivos");
            Console.WriteLine("3. Abrir un archivo");
            Console.WriteLine("4. Modificar un archivo");
            Console.WriteLine("5. Eliminar un archivo");
            Console.WriteLine("6. Recuperar un archivo");
            Console.WriteLine("7. Salir");

            Console.Write("Selecciona una opción: ");
            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    CreateFile();
                    break;
                case "2":
                    ListFiles();
                    break;
                case "3":
                    OpenFile();
                    break;
                case "4":
                    ModifyFile();
                    break;
                case "5":
                    DeleteFile();
                    break;
                case "6":
                    RecoverFile();
                    break;
                case "7":
                    return;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }
    }

    static void CreateFile()
    {
        Console.Write("Ingrese el nombre del archivo: ");
        string fileName = Console.ReadLine() ?? string.Empty; 
        Console.Write("Ingrese los datos a almacenar: ");
        string data = Console.ReadLine() ?? string.Empty;

        if (data.Length == 0)
        {
            Console.WriteLine("No se puede crear un archivo sin datos.");
            return;
        }

       
        FATEntry fatEntry = new FATEntry
        {
            FileName = fileName,
            InitialDataFile = string.Empty,
            TotalCharacters = data.Length,
            CreationDate = DateTime.Now
        };

        List<string> dataFiles = new List<string>();
        int index = 0;

    
        while (index < data.Length)
        {
            DataFile dataFile = new DataFile
            {
                Data = data.Substring(index, Math.Min(20, data.Length - index)),
                Eof = (index + 20 >= data.Length),
                NextDataFile = null
            };

            string dataFilePath = Path.Combine(directory, Guid.NewGuid().ToString() + ".json");

            if (dataFiles.Count > 0)
            {
               
                var previousFileData = JsonSerializer.Deserialize<DataFile>(File.ReadAllText(dataFiles[^1]))!;
                previousFileData.NextDataFile = dataFilePath;
                File.WriteAllText(dataFiles[^1], JsonSerializer.Serialize(previousFileData));
            }

            File.WriteAllText(dataFilePath, JsonSerializer.Serialize(dataFile));
            dataFiles.Add(dataFilePath);
            index += 20;
        }

    
        fatEntry.InitialDataFile = dataFiles[0];
        string fatFilePath = Path.Combine(directory, fileName + "_FAT.json");
        File.WriteAllText(fatFilePath, JsonSerializer.Serialize(fatEntry));

        Console.WriteLine($"Archivo '{fileName}' creado con éxito.");
    }

    static void ListFiles()
    {
        var fatFiles = Directory.GetFiles(directory, "*_FAT.json");

        foreach (var fatFile in fatFiles)
        {
            var fatEntry = JsonSerializer.Deserialize<FATEntry>(File.ReadAllText(fatFile));
            if (fatEntry != null && !fatEntry.IsInRecycleBin)
            {
                Console.WriteLine($"Nombre: {fatEntry.FileName}, Tamaño: {fatEntry.TotalCharacters} caracteres, " +
                                  $"Creación: {fatEntry.CreationDate}, Modificación: {fatEntry.ModificationDate}");
            }
        }
    }

    static void OpenFile()
    {
        ListFiles();
        Console.Write("Seleccione el nombre del archivo a abrir: ");
        string fileName = Console.ReadLine() ?? string.Empty;
        string fatFilePath = Path.Combine(directory, fileName + "_FAT.json");

        if (File.Exists(fatFilePath))
        {
            var fatEntry = JsonSerializer.Deserialize<FATEntry>(File.ReadAllText(fatFilePath));
            if (fatEntry != null && !fatEntry.IsInRecycleBin)
            {
                Console.WriteLine($"Nombre: {fatEntry.FileName}, Tamaño: {fatEntry.TotalCharacters}, Creación: {fatEntry.CreationDate}, Modificación: {fatEntry.ModificationDate}");

            
                string dataFilePath = fatEntry.InitialDataFile;
                while (dataFilePath != null)
                {
                    var dataFile = JsonSerializer.Deserialize<DataFile>(File.ReadAllText(dataFilePath))!;
                    Console.Write(dataFile.Data);
                    dataFilePath = dataFile.NextDataFile;
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("Archivo no encontrado.");
        }
    }

    static void ModifyFile()
    {
        ListFiles();
        Console.Write("Seleccione el nombre del archivo a modificar: ");
        string fileName = Console.ReadLine() ?? string.Empty;
        string fatFilePath = Path.Combine(directory, fileName + "_FAT.json");

        if (File.Exists(fatFilePath))
        {
            var fatEntry = JsonSerializer.Deserialize<FATEntry>(File.ReadAllText(fatFilePath));
            if (fatEntry != null && !fatEntry.IsInRecycleBin)
            {
                // Eliminar los archivos de datos viejos
                string dataFilePath = fatEntry.InitialDataFile;
                while (!string.IsNullOrEmpty(dataFilePath))
                {
                    var dataFile = JsonSerializer.Deserialize<DataFile>(File.ReadAllText(dataFilePath))!;
                    string nextFile = dataFile.NextDataFile;
                    File.Delete(dataFilePath);
                    dataFilePath = nextFile;
                }

             
                Console.Write("Ingrese los nuevos datos: ");
                string newContent = Console.ReadLine() ?? string.Empty;

                if (newContent.Length == 0)
                {
                    Console.WriteLine("No se puede modificar un archivo sin datos.");
                    return;
                }

              
                List<string> dataFiles = new List<string>();
                int index = 0;

                while (index < newContent.Length)
                {
                    DataFile dataFile = new DataFile
                    {
                        Data = newContent.Substring(index, Math.Min(20, newContent.Length - index)),
                        Eof = (index + 20 >= newContent.Length),
                        NextDataFile = null
                    };

                    string newDataFilePath = Path.Combine(directory, Guid.NewGuid().ToString() + ".json");

                    if (dataFiles.Count > 0)
                    {
                        var previousFileData = JsonSerializer.Deserialize<DataFile>(File.ReadAllText(dataFiles[^1]))!;
                        previousFileData.NextDataFile = newDataFilePath;
                        File.WriteAllText(dataFiles[^1], JsonSerializer.Serialize(previousFileData));
                    }

                    File.WriteAllText(newDataFilePath, JsonSerializer.Serialize(dataFile));
                    dataFiles.Add(newDataFilePath);
                    index += 20;
                }

                
                fatEntry.InitialDataFile = dataFiles[0];
                fatEntry.TotalCharacters = newContent.Length;
                fatEntry.ModificationDate = DateTime.Now;
                File.WriteAllText(fatFilePath, JsonSerializer.Serialize(fatEntry));

                Console.WriteLine($"Archivo '{fileName}' modificado con éxito.");
            }
        }
        else
        {
            Console.WriteLine("Archivo no encontrado.");
        }
    }

    static void DeleteFile()
    {
        ListFiles();
        Console.Write("Seleccione el nombre del archivo a eliminar: ");
        string fileName = Console.ReadLine() ?? string.Empty;
        string fatFilePath = Path.Combine(directory, fileName + "_FAT.json");

        if (File.Exists(fatFilePath))
        {
            var fatEntry = JsonSerializer.Deserialize<FATEntry>(File.ReadAllText(fatFilePath));
            if (fatEntry != null && !fatEntry.IsInRecycleBin)
            {
                fatEntry.IsInRecycleBin = true;
                fatEntry.DeletionDate = DateTime.Now;
                File.WriteAllText(fatFilePath, JsonSerializer.Serialize(fatEntry));
                Console.WriteLine($"Archivo '{fileName}' movido a la papelera de reciclaje.");
            }
        }
        else
        {
            Console.WriteLine("Archivo no encontrado.");
        }
    }

    static void RecoverFile()
    {
        var fatFiles = Directory.GetFiles(directory, "*_FAT.json");
        foreach (var fatFile in fatFiles)
        {
            var fatEntry = JsonSerializer.Deserialize<FATEntry>(File.ReadAllText(fatFile));
            if (fatEntry != null && fatEntry.IsInRecycleBin)
            {
                Console.WriteLine($"Nombre: {fatEntry.FileName}, Tamaño: {fatEntry.TotalCharacters} caracteres, Eliminado: {fatEntry.DeletionDate}");
            }
        }

        Console.Write("Seleccione el nombre del archivo a recuperar: ");
        string fileName = Console.ReadLine() ?? string.Empty;
        string fatFilePath = Path.Combine(directory, fileName + "_FAT.json");

        if (File.Exists(fatFilePath))
        {
            var fatEntry = JsonSerializer.Deserialize<FATEntry>(File.ReadAllText(fatFilePath));
            if (fatEntry != null && fatEntry.IsInRecycleBin)
            {
                fatEntry.IsInRecycleBin = false;
                fatEntry.ModificationDate = DateTime.Now;
                File.WriteAllText(fatFilePath, JsonSerializer.Serialize(fatEntry));
                Console.WriteLine($"Archivo '{fileName}' recuperado.");
            }
        }
        else
        {
            Console.WriteLine("Archivo no encontrado.");
        }
    }
}
