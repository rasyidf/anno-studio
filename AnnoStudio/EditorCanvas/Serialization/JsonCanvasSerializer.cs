using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;
using AnnoStudio.EditorCanvas.Objects;

namespace AnnoStudio.EditorCanvas.Serialization;

/// <summary>
/// JSON serializer for canvas documents with type registry.
/// </summary>
public class JsonCanvasSerializer : ICanvasSerializer
{
    private readonly Dictionary<string, Type> _typeRegistry = new();
    private readonly JsonSerializerOptions _options;

    public JsonCanvasSerializer()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new SKColorJsonConverter(),
                new SKPointJsonConverter(),
                new Transform2DJsonConverter(),
                new CanvasObjectJsonConverter(this)
            }
        };

        // Register default types
        RegisterType("Building", typeof(BuildingObject));
    }

    public void RegisterType(string typeId, Type type)
    {
        if (!typeof(ICanvasObject).IsAssignableFrom(type))
            throw new ArgumentException($"Type {type.Name} must implement ICanvasObject");

        _typeRegistry[typeId] = type;
    }

    public Type? GetTypeFromId(string typeId)
    {
        return _typeRegistry.TryGetValue(typeId, out var type) ? type : null;
    }

    public async Task<CanvasDocument> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var json = await JsonSerializer.DeserializeAsync<JsonDocument>(stream, _options, cancellationToken);
        if (json == null)
            throw new InvalidDataException("Failed to deserialize canvas document");

        var document = new CanvasDocument();

        // Deserialize objects
        if (json.RootElement.TryGetProperty("Objects", out var objectsArray))
        {
            foreach (var objElement in objectsArray.EnumerateArray())
            {
                var obj = DeserializeObjectInternal(objElement);
                if (obj != null)
                {
                    document.Objects.Add(obj);
                }
            }
        }

        // Deserialize metadata
        if (json.RootElement.TryGetProperty("Metadata", out var metadata))
        {
            if (metadata.TryGetProperty("Title", out var title))
                document.Metadata.Title = title.GetString() ?? string.Empty;

            if (metadata.TryGetProperty("Description", out var desc))
                document.Metadata.Description = desc.GetString() ?? string.Empty;

            if (metadata.TryGetProperty("Author", out var author))
                document.Metadata.Author = author.GetString() ?? string.Empty;
        }

        return document;
    }

    public async Task SerializeAsync(CanvasDocument document, Stream stream, CancellationToken cancellationToken = default)
    {
        var data = new
        {
            document.Version,
            Metadata = new
            {
                document.Metadata.Title,
                document.Metadata.Description,
                document.Metadata.Author,
                Created = document.Metadata.Created.ToString("O"),
                Modified = document.Metadata.Modified.ToString("O")
            },
            Objects = document.Objects.Select(obj => SerializeObject(obj)).ToList()
        };

        await JsonSerializer.SerializeAsync(stream, data, _options, cancellationToken);
    }

    public string Serialize(CanvasDocument document)
    {
        var data = new
        {
            document.Version,
            Metadata = new
            {
                document.Metadata.Title,
                document.Metadata.Description,
                document.Metadata.Author,
                Created = document.Metadata.Created.ToString("O"),
                Modified = document.Metadata.Modified.ToString("O")
            },
            Objects = document.Objects.Select(obj => SerializeObjectInternal(obj)).ToList()
        };

        return JsonSerializer.Serialize(data, _options);
    }

    public CanvasDocument Deserialize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var document = new CanvasDocument();

        // Deserialize objects
        if (doc.RootElement.TryGetProperty("Objects", out var objectsArray))
        {
            foreach (var objElement in objectsArray.EnumerateArray())
            {
                var obj = DeserializeObjectInternal(objElement);
                if (obj != null)
                {
                    document.Objects.Add(obj);
                }
            }
        }

        return document;
    }

    public string SerializeObject(ICanvasObject obj)
    {
        var properties = SerializeObjectInternal(obj);
        return JsonSerializer.Serialize(properties, _options);
    }

    public ICanvasObject DeserializeObject(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return DeserializeObjectInternal(doc.RootElement) ?? throw new InvalidDataException("Failed to deserialize object");
    }

    private object SerializeObjectInternal(ICanvasObject obj)
    {
        var properties = obj.GetProperties();
        properties["$type"] = obj.Type;

        return properties;
    }

    private ICanvasObject? DeserializeObjectInternal(JsonElement element)
    {
        if (!element.TryGetProperty("$type", out var typeProperty))
            return null;

        var typeId = typeProperty.GetString();
        if (typeId == null || !_typeRegistry.TryGetValue(typeId, out var type))
            return null;

        var obj = Activator.CreateInstance(type) as ICanvasObject;
        if (obj == null)
            return null;

        var properties = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name == "$type")
                continue;

            properties[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => property.Value.GetRawText()
            };
        }

        obj.SetProperties(properties);
        return obj;
    }
}

/// <summary>
/// JSON converter for SKColor.
/// </summary>
public class SKColorJsonConverter : JsonConverter<SKColor>
{
    public override SKColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var colorString = reader.GetString();
        if (colorString != null && SKColor.TryParse(colorString, out var color))
            return color;

        return SKColors.Transparent;
    }

    public override void Write(Utf8JsonWriter writer, SKColor value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// JSON converter for SKPoint.
/// </summary>
public class SKPointJsonConverter : JsonConverter<SKPoint>
{
    public override SKPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return SKPoint.Empty;

        float x = 0, y = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                if (propertyName == "X")
                    x = reader.GetSingle();
                else if (propertyName == "Y")
                    y = reader.GetSingle();
            }
        }

        return new SKPoint(x, y);
    }

    public override void Write(Utf8JsonWriter writer, SKPoint value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}

/// <summary>
/// JSON converter for Transform2D.
/// </summary>
public class Transform2DJsonConverter : JsonConverter<Transform2D>
{
    public override Transform2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var transform = new Transform2D();

        if (reader.TokenType != JsonTokenType.StartObject)
            return transform;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "Position":
                        transform.Position = JsonSerializer.Deserialize<SKPoint>(ref reader, options);
                        break;
                    case "Rotation":
                        transform.Rotation = reader.GetSingle();
                        break;
                    case "Scale":
                        transform.Scale = JsonSerializer.Deserialize<SKPoint>(ref reader, options);
                        break;
                }
            }
        }

        return transform;
    }

    public override void Write(Utf8JsonWriter writer, Transform2D value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        writer.WritePropertyName("Position");
        JsonSerializer.Serialize(writer, value.Position, options);
        
        writer.WriteNumber("Rotation", value.Rotation);
        
        writer.WritePropertyName("Scale");
        JsonSerializer.Serialize(writer, value.Scale, options);
        
        writer.WriteEndObject();
    }
}

/// <summary>
/// JSON converter for ICanvasObject with type discrimination.
/// </summary>
public class CanvasObjectJsonConverter : JsonConverter<ICanvasObject>
{
    private readonly JsonCanvasSerializer _serializer;

    public CanvasObjectJsonConverter(JsonCanvasSerializer serializer)
    {
        _serializer = serializer;
    }

    public override ICanvasObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var method = _serializer.GetType()
            .GetMethod("DeserializeObjectInternal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return method?.Invoke(_serializer, new object[] { doc.RootElement }) as ICanvasObject;
    }

    public override void Write(Utf8JsonWriter writer, ICanvasObject value, JsonSerializerOptions options)
    {
        var properties = value.GetProperties();
        properties["$type"] = value.Type;

        JsonSerializer.Serialize(writer, properties, options);
    }
}
