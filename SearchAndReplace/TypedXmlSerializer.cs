using System.Collections;
using System.IO;
using System.Xml.Serialization;

namespace CustomActions
{
    public class TypedXmlSerializer<T>
    {
        public void Serialize(string path, T toSerialize)
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var fileStream =
            new FileStream(path, FileMode.Create))
            {
                serializer.Serialize(fileStream, toSerialize);
                fileStream.Close();
            }
        }

        public T Deserialize(string path)
        {
            var serializer = new XmlSerializer(typeof(T));
            T persistedObject;
            using (var reader = new StreamReader(path))
            {
                persistedObject = (T)serializer.Deserialize(reader);
                reader.Close();
            }
            return persistedObject;
        }
    }
}