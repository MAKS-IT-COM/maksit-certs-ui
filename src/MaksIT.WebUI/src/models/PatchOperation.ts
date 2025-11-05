export enum PatchOperation {

  /// <summary>
  /// When you need to set or replace a normal field
  /// </summary>
  SetField,

  /// <summary>
  /// When you need to set a normal field to null
  /// </summary>
  RemoveField,

  /// <summary>
  /// When you need to add an item to a collection
  /// </summary>
  AddToCollection,

  /// <summary>
  /// When you need to remove an item from a collection
  /// </summary>
  RemoveFromCollection,
}