namespace TdStringsHelper;

internal class DataCollection<T> : List<T>, IListValue {
  ListValueKind IListValue.Kind =>
    ListValueKind.DataEntries;

  /* For getting entry type without file name access. */
  public Type ElementKind { get; set; }

  public DataCollection() => ElementKind = typeof(void);
}
