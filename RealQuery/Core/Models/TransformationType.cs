namespace RealQuery.Core.Models;

/// <summary>
/// Tipos de transformação suportados
/// </summary>
public enum TransformationType
{
  /// <summary>
  /// Importação de dados (Excel, CSV, etc.)
  /// </summary>
  Import,

  /// <summary>
  /// Exportação de dados
  /// </summary>
  Export,

  /// <summary>
  /// Filtro de linhas
  /// </summary>
  Filter,

  /// <summary>
  /// Ordenação
  /// </summary>
  Sort,

  /// <summary>
  /// Agrupamento
  /// </summary>
  GroupBy,

  /// <summary>
  /// Junção de dados
  /// </summary>
  Join,

  /// <summary>
  /// Seleção de colunas
  /// </summary>
  Select,

  /// <summary>
  /// Agregação (Sum, Count, etc.)
  /// </summary>
  Aggregate,

  /// <summary>
  /// Código C# customizado
  /// </summary>
  CSharpCode,

  /// <summary>
  /// Transformação customizada
  /// </summary>
  Custom
}