# Tài Liệu Luồng Xử Lý - Payment Card Key Validation System (Updated)

## Tổng Quan Hệ Thống

Hệ thống này được thiết kế để kiểm tra tính hợp lệ của các key mã hóa (DES, 3DES) được sử dụng trong ngành thanh toán thẻ. Hệ thống tuân thủ các chuẩn bảo mật của ngành tài chính, kiểm tra format, độ dài, odd parity bit và tính toán KCV (Key Check Value). **Hỗ trợ nhiều thuật toán mã hóa thông qua Strategy Pattern với khả năng runtime switching**.

---

## 1. Kiến Trúc Tổng Thể

### 1.1 Các Thành Phần Chính

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Layer                                 │
│                   KeyToolsController                             │
│  - ValidateSingle (POST /api/keytools/key)                      │
│  - ValidateBatch (POST /api/keytools/keys)                      │
│  - ValidateFile (POST /api/keytools/file)                       │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Service Layer                                │
│                  KeyQualityService                               │
│  - SetAlgorithm(CryptoAlgorithm): void                          │
│  - ValidateKey(string): KeyCheckResult                          │
│  - ValidateBatch(List<string>): BatchKeyCheckResult             │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                 Strategy Context Layer                           │
│                     CryptoContext                                │
│  - SetStrategy(CryptoAlgorithm): void                           │
│  - CurrentStrategy: ICryptoStrategy                             │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│             ICryptoStrategy (Interface)                          │
│  - CalculateKcv(string): string                                 │
│  - ValidateKeyStructure(string): KeyCheckResult                 │
│                                                                  │
│  Implementations (via BaseCryptoStrategy):                       │
│    ├── TripleDesStrategy                                         │
│    │    └── Validates 32 or 48 hex chars                        │
│    └── DesStrategy                                               │
│         └── Validates 16 hex chars                              │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Request Flow với Algorithm Selection

```
Frontend (User selects algorithm)
    ↓
    JSON Request: { "key": "...", "algorithm": 1 }
    ↓
Controller receives request
    ↓
    _keyQualityService.SetAlgorithm(request.Algorithm)
    ↓
CryptoContext.SetStrategy(algorithm)
    ├── algorithm = 1 → new TripleDesStrategy()
    └── algorithm = 2 → new DesStrategy()
    ↓
Service.ValidateKey() uses CurrentStrategy
    ↓
Strategy validates & calculates KCV
    ↓
Response with KCV and validation result
```

---

## 2. Chi Tiết DTOs (Data Transfer Objects)

### 2.1 Request DTOs

#### KeyRequest

```csharp
public class KeyRequest
{
    public string Key { get; set; } = string.Empty;
    public CryptoAlgorithm Algorithm { get; set; } = CryptoAlgorithm.TripleDes;
}
```

**Mục đích**: Nhận một key đơn lẻ từ client để kiểm tra, kèm theo thuật toán mã hóa.

**Ví dụ JSON**:

```json
{
  "key": "0123456789ABCDEFFEDCBA9876543210",
  "algorithm": 1
}
```

- `algorithm`: 1 = TripleDes, 2 = Des (mặc định: TripleDes)

#### BatchKeyRequest

```csharp
public class BatchKeyRequest
{
    public List<string> Keys { get; set; } = new();
    public CryptoAlgorithm Algorithm { get; set; } = CryptoAlgorithm.TripleDes;
}
```

**Mục đích**: Nhận nhiều keys cùng lúc để kiểm tra hàng loạt với cùng thuật toán.

**Ví dụ JSON**:

```json
{
  "keys": [
    "0123456789ABCDEFFEDCBA9876543210",
    "FEDCBA98765432100123456789ABCDEF"
  ],
  "algorithm": 1
}
```

#### FileUploadRequest

```csharp
public class FileUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public CryptoAlgorithm Algorithm { get; set; } = CryptoAlgorithm.TripleDes;
}
```

**Mục đích**: Upload file chứa keys kèm theo thuật toán mã hóa.

**Ví dụ Form Data**:

```javascript
const formData = new FormData();
formData.append("File", fileInput.files[0]);
formData.append("Algorithm", "2"); // DES
```

---

### 2.2 Response DTOs

#### KeyCheckResult

```csharp
public class KeyCheckResult
{
    public string InputKey { get; set; } = string.Empty;
    public bool IsValid => Status == KeyValidationStatus.Valid;
    public KeyValidationStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Kcv { get; set; }
}
```

**Ví dụ Response - Key hợp lệ (3DES)**:

```json
{
  "inputKey": "0123456789ABCDEFFEDCBA9876543210",
  "isValid": true,
  "status": 1,
  "message": "Key hợp lệ",
  "kcv": "8D2270"
}
```

**Ví dụ Response - Key hợp lệ (DES)**:

```json
{
  "inputKey": "0123456789ABCDEF",
  "isValid": true,
  "status": 1,
  "message": "Key hợp lệ",
  "kcv": "A1B2C3"
}
```

#### BatchKeyCheckResult

```csharp
public class BatchKeyCheckResult
{
    public int TotalKeys { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public List<KeyCheckResult> Details { get; set; }
}
```

---

## 3. Enum Definitions

### 3.1 CryptoAlgorithm (NEW)

```csharp
public enum CryptoAlgorithm
{
    [Description("3DES - 128/192 bit")]
    TripleDes = 1,

    [Description("DES - 64 bit")]
    Des = 2
}
```

**Mục đích**: Cho phép client chọn thuật toán mã hóa để validate và tính KCV.

**Sử dụng**:

- Frontend gửi `"algorithm": 1` hoặc `"algorithm": 2`
- Backend switch strategy tương ứng runtime

### 3.2 KeyValidationStatus

```csharp
public enum KeyValidationStatus
{
    Valid = 1,
    InvalidFormat = 2,
    InvalidLength = 3,    // DES: 16, 3DES: 32/48 hex chars
    BadParity = 4,
    WeakKey = 5
}
```

### 3.3 Message Enum

```csharp
public enum Message
{
    Emptykey = 1,
    InvalidFormat = 2,
    InvalidLength = 3,    // "Độ dài không đúng (DES: 16, 3DES: 32/48)"
    BadParity = 4,
    Valid = 5,
    WeakKey = 6
}
```

---

## 4. Luồng Xử Lý Chi Tiết

### 4.1 Luồng Validate Single Key với Algorithm Selection

```
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 1: Frontend - User chọn algorithm                              │
│                                                                      │
│ UI: Dropdown/Radio buttons                                          │
│ [ ] Triple DES (value=1)                                            │
│ [x] DES (value=2)                                                   │
│                                                                      │
│ POST /api/keytools/key                                              │
│ Body: {                                                              │
│   "key": "0123456789ABCDEF",                                        │
│   "algorithm": 2                                                    │
│ }                                                                    │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 2: KeyToolsController.ValidateSingle()                         │
│                                                                      │
│ - Kiểm tra request null và key rỗng                                 │
│ - Trim khoảng trắng: request.Key.Trim()                            │
│ - **Set algorithm từ request**:                                     │
│   _keyQualityService.SetAlgorithm(request.Algorithm)               │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 3: KeyQualityService.SetAlgorithm(algorithm)                  │
│                                                                      │
│ public void SetAlgorithm(CryptoAlgorithm algorithm)                │
│ {                                                                    │
│     _cryptoContext.SetStrategy(algorithm);                         │
│ }                                                                    │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 4: CryptoContext.SetStrategy(algorithm)                       │
│                                                                      │
│ _strategy = algorithm switch                                        │
│ {                                                                    │
│     CryptoAlgorithm.TripleDes => new TripleDesStrategy(),          │
│     CryptoAlgorithm.Des => new DesStrategy(),                      │
│     _ => throw new ArgumentException(...)                          │
│ };                                                                   │
│                                                                      │
│ → Tạo strategy instance tương ứng runtime                          │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 5: KeyQualityService.ValidateKey(rawKey)                      │
│                                                                      │
│ 5.1. Delegate validation to CurrentStrategy                         │
│      var result = _cryptoContext.CurrentStrategy                   │
│                     .ValidateKeyStructure(rawKey);                 │
│                                                                      │
│ 5.2. If validation passed, calculate KCV                            │
│      try {                                                          │
│          result.Kcv = _cryptoContext.CurrentStrategy               │
│                         .CalculateKcv(rawKey);                     │
│          result.Status = KeyValidationStatus.Valid;                │
│      }                                                              │
│      catch {                                                        │
│          result.Status = KeyValidationStatus.WeakKey;              │
│      }                                                              │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 6: Strategy.ValidateKeyStructure(rawKey)                      │
│         (BaseCryptoStrategy - Template Method Pattern)             │
│                                                                      │
│ 6.1. Common validations (tất cả algorithms):                        │
│      - Check null/empty                                             │
│      - Check hex format: regex "^[0-9a-fA-F]+$"                   │
│                                                                      │
│ 6.2. Algorithm-specific validations (hook method):                  │
│      → TripleDesStrategy.ValidateSpecificRules()                   │
│        - Length must be 32 or 48 hex chars                         │
│        - Check odd parity                                           │
│                                                                      │
│      → DesStrategy.ValidateSpecificRules()                         │
│        - Length must be 16 hex chars                               │
│        - Check odd parity                                           │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 7: Strategy.CalculateKcv(hexKey)                              │
│                                                                      │
│ For TripleDesStrategy:                                              │
│   - Convert hex → bytes (16 or 24 bytes)                           │
│   - Create TripleDES encryptor (ECB, no padding)                   │
│   - Encrypt zero block [0x00 * 8]                                  │
│   - Return first 6 hex chars of ciphertext                         │
│                                                                      │
│ For DesStrategy:                                                    │
│   - Convert hex → bytes (8 bytes)                                  │
│   - Create DES encryptor (ECB, no padding)                         │
│   - Encrypt zero block [0x00 * 8]                                  │
│   - Return first 6 hex chars of ciphertext                         │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 8: Controller trả về JSON response                             │
│ Ok(result) → HTTP 200                                               │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 5. Design Patterns Được Sử Dụng

### 5.1 Strategy Pattern ⭐

**Context**: Cho phép runtime algorithm switching.

```csharp
// Context Class
public class CryptoContext
{
    private ICryptoStrategy _strategy;

    public void SetStrategy(CryptoAlgorithm algorithm)
    {
        _strategy = algorithm switch
        {
            CryptoAlgorithm.TripleDes => new TripleDesStrategy(),
            CryptoAlgorithm.Des => new DesStrategy(),
            _ => throw new ArgumentException("Algorithm not supported")
        };
    }

    public ICryptoStrategy CurrentStrategy => _strategy;
}

// Strategy Interface
public interface ICryptoStrategy
{
    string CalculateKcv(string hexKey);
    KeyCheckResult ValidateKeyStructure(string rawKey);
}

// Concrete Strategies
public class TripleDesStrategy : BaseCryptoStrategy
{
    public override string CalculateKcv(string hexKey)
    {
        // 3DES-specific KCV calculation
    }

    protected override KeyCheckResult ValidateSpecificRules(...)
    {
        // 3DES: length must be 32 or 48
        if (key.Length != 32 && key.Length != 48) { ... }
    }
}

public class DesStrategy : BaseCryptoStrategy
{
    public override string CalculateKcv(string hexKey)
    {
        // DES-specific KCV calculation
    }

    protected override KeyCheckResult ValidateSpecificRules(...)
    {
        // DES: length must be 16
        if (key.Length != 16) { ... }
    }
}
```

**Lợi ích**:

- ✅ **Runtime switching**: User chọn algorithm qua API, không cần restart
- ✅ **Open/Closed Principle**: Thêm algorithm mới không sửa code cũ
- ✅ **Separation of Concerns**: Mỗi strategy tự quản lý logic riêng
- ✅ **Testability**: Dễ test từng strategy độc lập

### 5.2 Template Method Pattern ⭐

**Base class** định nghĩa skeleton, **subclasses** override specific steps.

```csharp
public abstract class BaseCryptoStrategy : ICryptoStrategy
{
    // Template method - defines algorithm skeleton
    public KeyCheckResult ValidateKeyStructure(string rawKey)
    {
        var result = new KeyCheckResult { InputKey = rawKey };

        // Step 1: Common validations
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            result.Status = KeyValidationStatus.InvalidFormat;
            result.Message = "Key rỗng";
            return result;
        }

        if (!Regex.IsMatch(rawKey, "^[0-9a-fA-F]+$"))
        {
            result.Status = KeyValidationStatus.InvalidFormat;
            result.Message = "Chứa ký tự không phải Hex";
            return result;
        }

        // Step 2: Algorithm-specific validation (hook method)
        return ValidateSpecificRules(rawKey, result);
    }

    // Hook method - override by subclasses
    protected abstract KeyCheckResult ValidateSpecificRules(
        string key, KeyCheckResult result);

    // Common helper method
    protected bool IsOddParity(string hexKey)
    {
        byte[] bytes = Convert.FromHexString(hexKey);
        foreach (var b in bytes)
        {
            int setBits = 0;
            int n = b;
            while (n > 0)
            {
                n &= (n - 1);
                setBits++;
            }
            if (setBits % 2 == 0) return false;
        }
        return true;
    }

    // Abstract method - must implement
    public abstract string CalculateKcv(string hexKey);
}
```

**Lợi ích**:

- ✅ **Code reuse**: Common logic (hex check, parity) chỉ viết 1 lần
- ✅ **Consistency**: Tất cả strategies follow cùng validation flow
- ✅ **Flexibility**: Subclass chỉ override phần khác biệt

### 5.3 Dependency Injection

```csharp
// Program.cs
builder.Services.AddScoped<IKeyQualityService, KeyQualityService>();
builder.Services.AddScoped<IInputSource, InputtxtStrategy>();

// Controller
public class KeyToolsController : ControllerBase
{
    private readonly IKeyQualityService _keyQualityService;

    public KeyToolsController(IKeyQualityService keyQualityService)
    {
        _keyQualityService = keyQualityService;
    }
}
```

**Lợi ích**:

- ✅ Loose coupling
- ✅ Testability (mock dependencies)
- ✅ Configuration tập trung

---

## 6. Validation Rules Chi Tiết

### Algorithm-Specific Rules

| Rule       | DES                        | 3DES                             |
| ---------- | -------------------------- | -------------------------------- |
| **Length** | 16 hex chars (8 bytes)     | 32 or 48 hex chars (16/24 bytes) |
| **Format** | Hex only (0-9, a-f, A-F)   | Hex only (0-9, a-f, A-F)         |
| **Parity** | Odd parity per byte        | Odd parity per byte              |
| **KCV**    | DES-ECB encrypt zero block | 3DES-ECB encrypt zero block      |

### Validation Steps

1. **Empty Check**

   - Điều kiện: `string.IsNullOrWhiteSpace(rawKey)`
   - Kết quả: `InvalidFormat`

2. **Hex Format Check**

   - Điều kiện: `!Regex.IsMatch(rawKey, "^[0-9a-fA-F]+$")`
   - Kết quả: `InvalidFormat`

3. **Length Check (Algorithm-Specific)**

   - **DES**: `key.Length != 16`
   - **3DES**: `key.Length != 32 && key.Length != 48`
   - Kết quả: `InvalidLength`

4. **Odd Parity Check**

   - Mỗi byte phải có số lượng bit 1 là lẻ
   - Kết quả: `BadParity`

5. **Weak Key Detection**
   - Crypto API throw exception (all zeros, weak patterns)
   - Kết quả: `WeakKey`

---

## 7. API Documentation Examples

### Example 1: Validate với 3DES

**Request**:

```http
POST /api/keytools/key HTTP/1.1
Content-Type: application/json

{
  "key": "0123456789ABCDEFFEDCBA9876543210",
  "algorithm": 1
}
```

**Response**:

```json
{
  "inputKey": "0123456789ABCDEFFEDCBA9876543210",
  "isValid": true,
  "status": 1,
  "message": "Key hợp lệ",
  "kcv": "8D2270"
}
```

### Example 2: Validate với DES

**Request**:

```http
POST /api/keytools/key HTTP/1.1
Content-Type: application/json

{
  "key": "0123456789ABCDEF",
  "algorithm": 2
}
```

**Response**:

```json
{
  "inputKey": "0123456789ABCDEF",
  "isValid": true,
  "status": 1,
  "message": "Key hợp lệ",
  "kcv": "A1B2C3"
}
```

### Example 3: Batch Validate

**Request**:

```http
POST /api/keytools/keys HTTP/1.1
Content-Type: application/json

{
  "keys": [
    "0123456789ABCDEF",
    "FEDCBA9876543210"
  ],
  "algorithm": 2
}
```

### Example 4: File Upload với Algorithm

**Request** (Form Data):

```javascript
const formData = new FormData();
formData.append("File", fileInput.files[0]);
formData.append("Algorithm", "1"); // 3DES

fetch("/api/keytools/file", {
  method: "POST",
  body: formData,
});
```

---

## 8. Frontend Integration Guide

### React/TypeScript Example

```typescript
// Types
enum CryptoAlgorithm {
  TripleDes = 1,
  Des = 2,
}

interface KeyRequest {
  key: string;
  algorithm: CryptoAlgorithm;
}

interface KeyCheckResult {
  inputKey: string;
  isValid: boolean;
  status: number;
  message: string;
  kcv?: string;
}

// Component
const KeyValidator: React.FC = () => {
  const [key, setKey] = useState("");
  const [algorithm, setAlgorithm] = useState<CryptoAlgorithm>(
    CryptoAlgorithm.TripleDes
  );
  const [result, setResult] = useState<KeyCheckResult | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const response = await fetch("/api/keytools/key", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ key, algorithm }),
    });

    const data: KeyCheckResult = await response.json();
    setResult(data);
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="text"
        value={key}
        onChange={(e) => setKey(e.target.value)}
        placeholder="Enter hex key"
      />

      <select
        value={algorithm}
        onChange={(e) => setAlgorithm(Number(e.target.value))}
      >
        <option value={CryptoAlgorithm.TripleDes}>
          Triple DES (32/48 chars)
        </option>
        <option value={CryptoAlgorithm.Des}>DES (16 chars)</option>
      </select>

      <button type="submit">Validate</button>

      {result && (
        <div className={result.isValid ? "success" : "error"}>
          <p>Status: {result.message}</p>
          {result.kcv && <p>KCV: {result.kcv}</p>}
        </div>
      )}
    </form>
  );
};
```

---

## 9. Extension Points

### Thêm Algorithm Mới (VD: AES)

**Bước 1**: Thêm enum value

```csharp
public enum CryptoAlgorithm
{
    TripleDes = 1,
    Des = 2,
    Aes = 3  // NEW
}
```

**Bước 2**: Tạo strategy class

```csharp
public class AesStrategy : BaseCryptoStrategy
{
    public override string CalculateKcv(string hexKey)
    {
        byte[] keyBytes = Convert.FromHexString(hexKey);
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using var encryptor = aes.CreateEncryptor();
        byte[] zeroBlock = new byte[16]; // AES block size
        byte[] result = encryptor.TransformFinalBlock(zeroBlock, 0, 16);

        return Convert.ToHexString(result)[..6];
    }

    protected override KeyCheckResult ValidateSpecificRules(
        string key, KeyCheckResult result)
    {
        // AES: 32, 48, or 64 hex chars (128/192/256 bit)
        if (key.Length != 32 && key.Length != 48 && key.Length != 64)
        {
            result.Status = KeyValidationStatus.InvalidLength;
            result.Message = "AES key must be 128/192/256 bit";
            return result;
        }

        // AES không yêu cầu odd parity
        return result;
    }
}
```

**Bước 3**: Cập nhật CryptoContext

```csharp
public void SetStrategy(CryptoAlgorithm algorithm)
{
    _strategy = algorithm switch
    {
        CryptoAlgorithm.TripleDes => new TripleDesStrategy(),
        CryptoAlgorithm.Des => new DesStrategy(),
        CryptoAlgorithm.Aes => new AesStrategy(),  // ADD
        _ => throw new ArgumentException("Algorithm not supported")
    };
}
```

**Done!** Không cần sửa Controller, Service, hoặc bất kỳ logic nào khác.

---

## 10. Performance & Security

### Performance

- **Batch processing**: Sequential (có thể parallel hóa với `Parallel.ForEach`)
- **Memory**: Stream-based file upload (không load toàn bộ vào RAM)
- **Crypto**: Native .NET crypto APIs (optimized)

### Security

- ✅ Keys không được log
- ✅ Keys chỉ tồn tại trong memory trong quá trình xử lý
- ✅ KCV không tiết lộ key gốc
- ✅ Input validation chặt chẽ (regex, length, parity)
- ⚠️ HTTPS bắt buộc trong production

---

## 11. Tóm Tắt

**Tính năng chính**:

- ✅ Hỗ trợ nhiều thuật toán: DES, 3DES (dễ mở rộng AES, RSA...)
- ✅ Runtime algorithm selection từ client
- ✅ Validate: format, length, parity, weak keys
- ✅ Calculate KCV chuẩn ngành tài chính
- ✅ 3 endpoints: single key, batch, file upload

**Design patterns**:

- ✅ Strategy Pattern (runtime switching)
- ✅ Template Method Pattern (code reuse)
- ✅ Dependency Injection (testability)

**Request flow**:

```
Client chọn algorithm → Request với algorithm field →
Controller set algorithm → CryptoContext switch strategy →
Strategy validate & calculate KCV → Response với KCV
```

---

**Tài liệu này mô tả đầy đủ hệ thống sau khi implement Strategy Pattern với DES support.**
