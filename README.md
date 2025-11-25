# Payment Card Key Validation System

Hệ thống kiểm tra tính hợp lệ của các key mã hóa được sử dụng trong ngành thanh toán thẻ.

## Tính năng

- ✅ **Validate Single Key**: Kiểm tra một key đơn lẻ
- ✅ **Validate Batch Keys**: Kiểm tra nhiều keys cùng lúc
- ✅ **Validate from File**: Upload file text chứa danh sách keys
- ✅ **Multiple Algorithms**: Hiện tại hổ trợ DES và 3DES
- ✅ **KCV Calculation**: Tính toán Key Check Value theo chuẩn ISO
- ✅ **Odd Parity Check**: Kiểm tra odd parity bit cho từng byte
- ✅ **RESTful API**: Swagger/OpenAPI documentation

## Kiến trúc

Hệ thống sử dụng **3-Layer Architecture** với **Strategy Pattern**:

```
┌─────────────────────┐
│   API Layer         │  Controllers
│   (REST API)        │
└──────────┬──────────┘
           │
┌──────────▼──────────┐
│   Service Layer     │  Business Logic
│   (Validation)      │
└──────────┬──────────┘
           │
┌──────────▼──────────┐
│   Strategy Layer    │  Crypto Algorithms
│   (DES, 3DES)       │
└─────────────────────┘
```

### Design Patterns

- **Strategy Pattern**: Linh hoạt chọn thuật toán mã hóa (DES/3DES)
- **Template Method Pattern**: Base validation logic
- **Factory Pattern**: Tạo strategy instances
- **Dependency Injection**: IoC container

## Công nghệ

- **Framework**: ASP.NET Core 8.0
- **Language**: C# 12
- **API Documentation**: Swagger/OpenAPI
- **Security**: .NET Cryptography APIs
- **Frontend**: React/Vue (optional)

## Cài đặt

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 hoặc VS Code
- Node.js 18+ (nếu chạy frontend)

### Clone Repository

```bash
git clone https://github.com/Waito3007/PaymentcardtoolsBE.git
cd paymentcardtools
```

### Restore Dependencies

```bash
dotnet restore
```

### Build Project

```bash
dotnet build
```

### Run Application

```bash
cd Paymentcardtools
dotnet run
```

API sẽ chạy tại: `https://localhost:7069`

## 📖 API Documentation

### 1. Validate Single Key

**Endpoint**: `POST /api/keytools/key`

**Request Body**:
```json
{
  "key": "0123456789ABCDEFFEDCBA9876543210",
  "algorithm": 1
}
```

**Response**:
```json
{
  "inputKey": "0123456789ABCDEFFEDCBA9876543210",
  "kcv": "A1B2C3",
  "status": 1,
  "message": "Key hợp lệ"
}
```

### 2. Validate Batch Keys

**Endpoint**: `POST /api/keytools/keys`

**Request Body**:
```json
{
  "keys": [
    "0123456789ABCDEFFEDCBA9876543210",
    "FEDCBA98765432100123456789ABCDEF"
  ],
  "algorithm": 1
}
```

**Response**:
```json
{
  "details": [
    {
      "inputKey": "0123456789ABCDEFFEDCBA9876543210",
      "kcv": "A1B2C3",
      "status": 1,
      "message": "Key hợp lệ"
    },
    {
      "inputKey": "FEDCBA98765432100123456789ABCDEF",
      "kcv": "D4E5F6",
      "status": 1,
      "message": "Key hợp lệ"
    }
  ],
  "totalKeys": 2,
  "validKeys": 2,
  "invalidKeys": 0
}
```

### 3. Validate from File

**Endpoint**: `POST /api/keytools/file`

**Request**: `multipart/form-data`
- `file`: Text file chứa keys (mỗi key một dòng)
- `algorithm`: 1 (3DES) hoặc 2 (DES)

**Response**: Tương tự Batch Keys

### Algorithm Types

| Value | Algorithm | Key Length |
|-------|-----------|------------|
| `1` | 3DES (Triple DES) | 32 hoặc 48 hex chars |
| `2` | DES | 16 hex chars |

## 🔍 Validation Rules

### 1. Format Check
- ✅ Chỉ chứa ký tự hex (0-9, A-F, a-f)
- ✅ Không có khoảng trắng

### 2. Length Check
- **DES**: 16 hex chars (8 bytes)
- **3DES**: 32 hex chars (16 bytes) hoặc 48 hex chars (24 bytes)

### 3. Odd Parity Check
Mỗi byte phải có số bit `1` là số lẻ:

```
Example: 0x01 = 0000 0001 → 1 bit → ✅ Valid
         0x03 = 0000 0011 → 2 bits → ❌ Invalid
```

### 4. Weak Key Detection
Phát hiện các weak keys theo chuẩn DES:
- All zeros: `0000000000000000`
- All ones: `FFFFFFFFFFFFFFFF`
- Semi-weak keys

### 5. KCV Calculation
Key Check Value được tính bằng cách:
1. Encrypt block zero (`0x00...00`) bằng key
2. Lấy 3 bytes đầu của kết quả mã hóa
3. Chuyển sang hex string

## 🧪 Testing

### Manual Testing with curl

**Test Single Key (3DES)**:
```bash
curl -X POST https://localhost:7069/api/keytools/key \
  -H "Content-Type: application/json" \
  -d '{"key":"0123456789ABCDEFFEDCBA9876543210","algorithm":1}'
```

**Test Single Key (DES)**:
```bash
curl -X POST https://localhost:7069/api/keytools/key \
  -H "Content-Type: application/json" \
  -d '{"key":"0123456789ABCDEF","algorithm":2}'
```

**Test File Upload**:
```bash
curl -X POST https://localhost:7069/api/keytools/file \
  -F "file=@keys.txt" \
  -F "algorithm=1"
```

### Test với Swagger UI

Truy cập: `https://localhost:7069/swagger`

## Cấu trúc thư mục

```
Paymentcardtools/
├── Controller/
│   └── KeyToolsController.cs       # REST API endpoints
├── Service/
│   ├── Interface/
│   │   └── IKeyQualityService.cs   # Service contract
│   └── KeyQualityService.cs        # Business logic
├── Extension/
│   ├── Interface/
│   │   ├── ICryptoStrategy.cs      # Strategy interface
│   │   └── IInputSource.cs         # File input interface
│   ├── Strategies/
│   │   ├── BaseCryptoStrategy.cs   # Base validation
│   │   ├── DesStrategy.cs          # DES implementation
│   │   └── TripleDesStrategy.cs    # 3DES implementation
│   ├── CryptoContext.cs            # Strategy context
│   ├── Inputtxt.cs                 # Text file reader
│   └── Extension.cs                # Helper methods
├── Models/
│   ├── DataModel/
│   │   ├── KeyCheckResult.cs       # Single key result
│   │   ├── BatchKeyCheckResult.cs  # Batch result
│   │   ├── KeyRequest.cs           # Request DTOs
│   │   └── BatchKeyRequest.cs
│   └── Enum/
│       ├── CryptoAlgorithm.cs      # Algorithm enum
│       ├── KeyValidationStatus.cs  # Status enum
│       └── Message.cs              # Message enum
├── doc/
│   └── processing-flow-updated.md  # Technical documentation
└── Program.cs                      # Application entry point
```

## Security Considerations

1. **Key Storage**: Keys chỉ tồn tại trong memory khi validate, không lưu trữ
2. **HTTPS Only**: Production phải dùng HTTPS
3. **CORS**: Cấu hình CORS chặt chẽ cho frontend
4. **Input Validation**: Validate tất cả inputs trước khi xử lý
5. **Rate Limiting**: Implement rate limiting để chống DoS
6. **Logging**: Không log keys ra file/console

## Frontend Integration

### React Example

```typescript
const validateKey = async (key: string, algorithm: number) => {
  const response = await fetch('https://localhost:7069/api/keytools/key', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ key, algorithm })
  });
  return await response.json();
};

// Usage
const result = await validateKey('0123456789ABCDEFFEDCBA9876543210', 1);
console.log(result.kcv); // "A1B2C3"
```

### File Upload Example

```typescript
const validateFile = async (file: File, algorithm: number) => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('algorithm', algorithm.toString());
  
  const response = await fetch('https://localhost:7069/api/keytools/file', {
    method: 'POST',
    body: formData
  });
  return await response.json();
};
```

## 🛠️ Development

### Thêm Algorithm mới

1. Tạo strategy class:

```csharp
public class AesStrategy : BaseCryptoStrategy
{
    protected override int[] ValidKeyLengths => new[] { 32, 48, 64 }; // AES-128/192/256
    
    public override string CalculateKcv(string hexKey)
    {
        // AES KCV implementation
    }
}
```

2. Thêm vào enum:

```csharp
public enum CryptoAlgorithm
{
    TripleDes = 1,
    Des = 2,
    Aes = 3  // ← New
}
```

3. Cập nhật CryptoContext:

```csharp
_strategy = algorithm switch
{
    CryptoAlgorithm.TripleDes => new TripleDesStrategy(),
    CryptoAlgorithm.Des => new DesStrategy(),
    CryptoAlgorithm.Aes => new AesStrategy(),  // ← New
    _ => throw new ArgumentException()
};
```
## 🐛 Troubleshooting

### Lỗi "Invalid key format"
- Kiểm tra key chỉ chứa hex chars (0-9, A-F)
- Xóa khoảng trắng, ký tự đặc biệt

### Lỗi "Invalid key length"
- DES: 16 chars
- 3DES: 32 hoặc 48 chars

### Lỗi "Odd parity check failed"
- Mỗi byte phải có số bit 1 là lẻ
- Sử dụng tool adjust parity nếu cần

### Lỗi "Weak key detected"
- Key bị cấm theo chuẩn DES
- Generate key mới

## 📝 License

MIT License - See LICENSE file for details

## 👥 Contributors

- Sang Vu -

## 📧 Contact

- Email: vuphanhoaisang@gmail.com
- GitHub: [https://github.com/Waito3007](https://github.com/Waito3007)
- Documentation: [https://github.com/Waito3007/PaymentcardtoolsBE/tree/main/doc](https://github.com/Waito3007/PaymentcardtoolsBE/tree/main/doc)

## 🔗 Related Links

- [ISO 9564 Standard](https://www.iso.org/standard/77034.html)
- [PCI DSS Requirements](https://www.pcisecuritystandards.org/)
- [ANSI X9.24 Key Management](https://webstore.ansi.org/)

---

**⚠️ Disclaimer**: Tool này chỉ dùng cho mục đích testing và development. Không sử dụng cho production keys trong môi trường không bảo mật.
