namespace TranslationHelper;

internal class UniqueStreamWriter : StreamWriter {
  private Dictionary<string, uint> m_tracked_string_indices = new();
  private uint m_current_index;

  public UniqueStreamWriter(string path) : base(path)
  {}

  public new uint WriteLine(string value) {
    /* It's kinda jank that we're overriding one overload, but it is
       all we need.
    */
    if (m_tracked_string_indices.ContainsKey(value))
      return m_tracked_string_indices[value];

    uint previous = m_current_index;
    base.WriteLine(value);
    m_tracked_string_indices[value] = previous;

    /* Once we've written the line, we can then increment our string
       index.
    */
    m_current_index++;

    /* At this point, the string has been indexed, so we can provide
       its index. 
    */
    return previous;
  }
}
