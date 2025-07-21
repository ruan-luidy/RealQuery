using System.Data;

namespace RealQuery.Core.Services;

/// <summary>
/// Interface base para serviços de dados (Excel, CSV, etc.)
/// </summary>
public interface IDataService
{
  /// <summary>
  /// Importa dados de um arquivo
  /// </summary>
  Task<DataTable> ImportAsync(string filePath);

  /// <summary>
  /// Exporta dados para um arquivo
  /// </summary>
  Task ExportAsync(DataTable data, string filePath);

  /// <summary>
  /// Verifica se o arquivo é suportado por este serviço
  /// </summary>
  bool CanHandle(string filePath);

  /// <summary>
  /// Obtem as extensoes suportadas
  /// </summary>
  string[] SupportedExtensions { get; }
}
