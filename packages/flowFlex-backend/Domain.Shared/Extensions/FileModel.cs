using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Extensions;

/*
  {
"id": "1938550277674254336",
"uid": "1751021269463",
"name": "head.png",
"size": "70621",
"type": "image/png",
"status": "success",
"response": {
  "id": "1938550277674254336",
  "uid": "1751021269463",
  "name": "head.png",
  "size": "70621",
  "type": "image/png",
  "status": "success"
}
}
 */
public struct FileModel
{
    public long Id { get; set; }

    public string Uid { get; set; }

    public string Name { get; set; }

    public int Size { get; set; }

    public string Type { get; set; }

    public string Status { get; set; }
}
