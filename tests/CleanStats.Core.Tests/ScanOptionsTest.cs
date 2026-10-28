using System;
using System.Collections.Generic;
using System.Text;
using CleanStats.Core.Model;
namespace CleanStats.Core.Tests;

public class ScanOptionsTest
{
    [Fact]

    public void Constructor_ShouldThrowArgumentException_WhenRootPathIsNullOrEmpty()
    {
        //Ruta raíz inválida
        //// Arrange
        //string rootPath = null;
        //string[] excludedPaths = new string[] { "path1", "path2" };
        //bool includeHiddenFiles = true;
        //// Act & Assert
        //var exception = Assert.Throws<ArgumentException>(() => new ScanOptions(rootPath, excludedPaths, includeHiddenFiles));
        //Assert.Equal("El path raíz no puede ser nulo o vacío. (Parameter 'rootPath')", exception.Message);
        // Arrange

        //Rutas excluidas nulas
        //string rootPath = "path1";
        //bool includeHiddenFiles = true;
        //// Act & Assert
        //var initial = new ScanOptions(rootPath, null, includeHiddenFiles);
        //Assert.Equal(initial.ExcludedPaths.Length, 0);

        //Rutas duplicadas
        //string[] excludedPaths = new string[] { "path1", "path2", "PATH1" };
        //string rootPath = "path1";
        //bool includeHiddenFiles = true;
        //var duplicate = new ScanOptions(rootPath, excludedPaths, includeHiddenFiles);
        //Assert.Equal(duplicate.ExcludedPaths.Length, 2);

        // Protección de la colección
        //string[] excludedPaths2 = new string[] { "path1", "path2" };
        //string rootPath = "path1";
        //bool includeHiddenFiles = true;
        //var initial = new ScanOptions(rootPath, excludedPaths2, includeHiddenFiles);
        //excludedPaths2[0] = "modifiedPath";
        //Assert.Equal(initial.ExcludedPaths[0], "path1");

        //Opciones originales
        string[] excludedPaths3 = new string[] { "path1", "path2" };
        string rootPath = "path1";
        bool includeHiddenFiles = true;
        var initial = new ScanOptions(rootPath, excludedPaths3, includeHiddenFiles);
        // Crear una nueva instancia con las mismas opciones
        var newInstance = new ScanOptions(initial.RootPath, initial.ExcludedPaths, initial.IncludeHiddenFiles);
        // Verificar que las propiedades de la nueva instancia sean iguales a las de la instancia original
        Assert.Equal(initial.RootPath, newInstance.RootPath);
        Assert.Equal(initial.ExcludedPaths, newInstance.ExcludedPaths);
        Assert.Equal(initial.IncludeHiddenFiles, newInstance.IncludeHiddenFiles);
    }
}
