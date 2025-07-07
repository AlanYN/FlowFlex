namespace FlowFlex.Infrastructure;

public static class CustomerTagsRuler
{
    public static long SetCombineTag(this List<int> tags)
    {
        long result = tags[0];
        for (var i = 1; i < tags.Count; i++)
        {
            result |= (uint)tags[i];
        }
        return result;
    }

    public static List<int> GetTags(this long tags)
    {
        var tagLength = Convert.ToString(tags, 2).Length;
        var tagsList = new List<int>();

        for (int i = 0; i <= tagLength; i++)
        {
            if ((tags & (int)Math.Pow(2, i)) >> i == 1)
            {
                tagsList.Add((int)Math.Pow(2, i));
            }
        }

        return tagsList;
    }
}
