# Tài Liệu Luồng Xử Lý - Payment Card Key Validation System

## Tổng Quan Hệ Thống

Hệ thống này được thiết kế để kiểm tra tính hợp lệ của các key mã hóa (DES, 3DES) được sử dụng trong ngành thanh toán thẻ. Hệ thống tuân thủ các chuẩn bảo mật của ngành tài chính, kiểm tra format, độ dài, odd parity bit và tính toán KCV (Key Check Value). Hỗ trợ nhiều thuật toán mã hóa thông qua Strategy Pattern.

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
│                   Strategy Pattern Layer                         │
│                     CryptoContext                                │
│  - SetStrategy(CryptoAlgorithm): void                           │
│  - CurrentStrategy: ICryptoStrategy                             │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                 ICryptoStrategy (Interface)                      │
│  - CalculateKcv(string): string                                 │
│  - ValidateKeyStructure(string): KeyCheckResult                 │
│                                                                  │
│  Implementations:                                                │
│    ├── TripleDesStrategy (32 or 48 hex chars)                   │
│    └── DesStrategy (16 hex chars)                               │
└─────────────────────────────────────────────────────────────────┘
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
    "FEDCBA98765432100123456789ABCDEF",
    "112233445566778899AABBCCDDEEFF00"
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

```
File: keys.txt
Algorithm: 2
```

---

### 2.2 Response DTOs

#### KeyCheckResult

```csharp
public class KeyCheckResult
{
    public string InputKey { get; set; } = string.Empty;      // Key đầu vào
    public bool IsValid => Status == KeyValidationStatus.Valid; // Trạng thái hợp lệ
    public KeyValidationStatus Status { get; set; }            // Mã trạng thái
    public string Message { get; set; } = string.Empty;        // Thông báo chi tiết
    public string? Kcv { get; set; }                          // Key Check Value (6 ký tự hex)
}
```

**Ví dụ Response - Key hợp lệ**:

```json
{
  "inputKey": "0123456789ABCDEFFEDCBA9876543210",
  "isValid": true,
  "status": 1,
  "message": "Key hợp lệ",
  "kcv": "A1B2C3"
}
```

**Ví dụ Response - Key sai format**:

```json
{
  "inputKey": "GHIJK12345",
  "isValid": false,
  "status": 2,
  "message": "Chứa ký tự không phải Hex",
  "kcv": null
}
```

#### BatchKeyCheckResult

```csharp
public class BatchKeyCheckResult
{
    public int TotalKeys { get; set; }                        // Tổng số key
    public int ValidCount { get; set; }                       // Số key hợp lệ
    public int InvalidCount { get; set; }                     // Số key không hợp lệ
    public List<KeyCheckResult> Details { get; set; }         // Chi tiết từng key
}
```

**Ví dụ Response**:

```json
{
  "totalKeys": 3,
  "validCount": 1,
  "invalidCount": 2,
  "details": [
    {
      "inputKey": "0123456789ABCDEFFEDCBA9876543210",
      "isValid": true,
      "status": 1,
      "message": "Key hợp lệ",
      "kcv": "A1B2C3"
    },
    {
      "inputKey": "1234567890ABCDEF",
      "isValid": false,
      "status": 3,
      "message": "Độ dài không đúng chuẩn 3DES (32/48)",
      "kcv": null
    }
  ]
}
```

---

## 3. Enum Definitions

### 3.1 CryptoAlgorithm

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

### 3.2 KeyValidationStatus

### 3.2 KeyValidationStatus

```csharp
public enum KeyValidationStatus
{
    Valid = 1,           // Key hợp lệ
    InvalidFormat = 2,   // Lỗi format chứa ký tự ko phải hex
    InvalidLength = 3,   // Lỗi độ dài (DES: 16, 3DES: 32/48 ký tự)
    BadParity = 4,       // Sai bit chẵn lẻ
    WeakKey = 5          // Key yếu, dễ đoán
}
```

### 3.3 Message Enum

```csharp
public enum Message
{
    Emptykey = 1,        // "Key rỗng"
    InvalidFormat = 2,   // "Chứa ký tự không phải Hex"
    InvalidLength = 3,   // "Độ dài không đúng (DES: 16, 3DES: 32/48)"
    BadParity = 4,       // "Sai bit chẵn lẻ (Odd Parity)"
    Valid = 5,           // "Key hợp lệ"
    WeakKey = 6          // "Key yếu hoặc lỗi thuật toán"
}
```

---

## 4. Luồng Xử Lý Chi Tiết

### 4.1 Luồng Validate Single Key (POST /api/keytools/key)

```
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 1: Client gửi request                                          │
│ POST /api/keytools/key                                              │
│ Body: {                                                              │
│   "key": "0123456789ABCDEFFEDCBA9876543210",                       │
│   "algorithm": 1                                                    │
│ }                                                                    │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 2: KeyToolsController.ValidateSingle()                         │
│ - Kiểm tra request null và key rỗng                                 │
│ - Trim khoảng trắng: request.Key.Trim()                            │
│ - Set algorithm: _keyQualityService.SetAlgorithm(request.Algorithm)│
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 3: KeyQualityService.SetAlgorithm(algorithm)                  │
│ - CryptoContext.SetStrategy(algorithm)                             │
│ - Switch strategy:                                                  │
│   * algorithm = 1 → new TripleDesStrategy()                        │
│   * algorithm = 2 → new DesStrategy()                              │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 4: KeyQualityService.ValidateKey(rawKey)                      │
│                                                                      │
│ 4.1. Delegate to CurrentStrategy                                    │
│      result = _cryptoContext.CurrentStrategy                       │
│                .ValidateKeyStructure(rawKey)                       │
│                                                                      │
│ 4.2. Strategy validates (BaseCryptoStrategy logic):                │
│      - Check empty/null                                             │
│      - Check hex format (regex ^[0-9a-fA-F]+$)                    │
│      - Check algorithm-specific length:                             │
│        * DES: 16 hex chars (8 bytes)                               │
│        * 3DES: 32 or 48 hex chars (16 or 24 bytes)                │
│      - Check odd parity                                             │
│                                                                      │
│ 4.3. If structure valid, calculate KCV                              │
│      try {                                                          │
│          result.Kcv = _cryptoContext.CurrentStrategy               │
│                         .CalculateKcv(rawKey)                      │
│          Status = Valid                                             │
│      }                                                              │
│      catch {                                                        │
│          Status = WeakKey                                           │
│      }                                                              │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 5: Strategy.CalculateKcv(hexKey)                              │
│                                                                      │
│ For TripleDesStrategy:                                              │
│   - Convert hex → bytes                                             │
│   - Create TripleDES encryptor (ECB, no padding)                   │
│   - Encrypt zero block (8 bytes)                                    │
│   - Return first 6 hex chars                                        │
│                                                                      │
│ For DesStrategy:                                                    │
│   - Convert hex → bytes (must be 8 bytes)                          │
│   - Create DES encryptor (ECB, no padding)                         │
│   - Encrypt zero block (8 bytes)                                    │
│   - Return first 6 hex chars                                        │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BƯỚC 6: Controller trả về JSON response                             │
│ Ok(result) → HTTP 200                                               │
└─────────────────────────────────────────────────────────────────────┘
```

---

### 4.2 Chi Tiết Thuật Toán Odd Parity Check

```csharp
private bool IsOddParity(string hexKey)
{
    byte[] bytes = Convert.FromHexString(hexKey);

    foreach (var b in bytes)
    {
        int setBits = 0;
        int n = b;

        // Đếm số bit 1 trong byte (Brian Kernighan's Algorithm)
        while (n > 0)
        {
            n &= (n - 1);  // Xóa bit 1 thấp nhất
            setBits++;
        }

        // Nếu số bit 1 là CHẴN → sai odd parity
        if (setBits % 2 == 0)
            return false;
    }

    return true;
}
```

**Giải thích**:

- Mỗi byte trong key phải có số lượng bit 1 là **LẺ** (odd)
- Thuật toán Brian Kernighan: `n &= (n - 1)` xóa bit 1 thấp nhất
- Ví dụ:
  - Byte `0x8F` = `10001111` → 5 bit 1 (lẻ) → ✅ PASS
  - Byte `0x8E` = `10001110` → 4 bit 1 (chẵn) → ❌ FAIL

**Tại sao phải kiểm tra Odd Parity?**

- Chuẩn DES/3DES yêu cầu mỗi byte key có odd parity bit
- Bit cuối cùng của mỗi byte là parity bit
- Giúp phát hiện lỗi truyền dẫn hoặc nhập liệu sai

---

### 4.3 Chi Tiết Tính KCV (Key Check Value)

**KCV là gì?**

- Key Check Value = 3 bytes đầu tiên của việc mã hóa block 0x00...00
- Dùng để xác minh key đã được nhập/truyền đúng hay chưa
- Không tiết lộ thông tin về key gốc

**Quy trình tính KCV**:

```
Input: Key = "0123456789ABCDEFFEDCBA9876543210" (32 hex chars)
      ↓
Step 1: Convert to bytes
      → [0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF,
         0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10]
      ↓
Step 2: Encrypt zero block with 3DES-ECB
      Zero Block = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]
      ↓
Step 3: Get cipher result (8 bytes)
      Example: [0xA1, 0xB2, 0xC3, 0xD4, 0xE5, 0xF6, 0x78, 0x90]
      ↓
Step 4: Take first 3 bytes (6 hex chars)
      KCV = "A1B2C3"
```

**Lý do sử dụng ECB mode**:

- ECB (Electronic Codebook) mã hóa từng block độc lập
- Không cần IV (Initialization Vector)
- Deterministic: cùng key + plaintext → cùng ciphertext
- Phù hợp cho tính KCV vì chỉ mã hóa 1 block duy nhất

---

### 4.4 Luồng Validate Batch Keys (POST /api/keytools/keys)

```
┌─────────────────────────────────────────────────────────────────────┐
│ Client gửi request                                                   │
│ POST /api/keytools/keys                                             │
│ Body: {                                                              │
│   "keys": [                                                          │
│     "0123456789ABCDEFFEDCBA9876543210",                            │
│     "FEDCBA98765432100123456789ABCDEF"                             │
│   ],                                                                 │
│   "algorithm": 1                                                    │
│ }                                                                    │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ KeyToolsController.ValidateBatch()                                  │
│ - Kiểm tra request.Keys có null hoặc rỗng không                    │
│ - Set algorithm: _keyQualityService.SetAlgorithm(request.Algorithm)│
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ KeyQualityService.ValidateBatch(rawKeys)                           │
│                                                                      │
│ var details = new List<KeyCheckResult>();                          │
│ foreach (var key in rawKeys)                                        │
│ {                                                                    │
│     details.Add(ValidateKey(key));  // Gọi validate cho từng key   │
│ }                                                                    │
│                                                                      │
│ return new BatchKeyCheckResult(details);                           │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BatchKeyCheckResult constructor                                     │
│                                                                      │
│ Details = details;                                                  │
│ TotalKeys = details.Count;                                          │
│ ValidCount = details.Count(x => x.IsValid);                        │
│ InvalidCount = details.Count(x => !x.IsValid);                     │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Controller trả về JSON với thống kê + chi tiết                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

### 4.5 Luồng Validate từ File (POST /api/keytools/file)

```
┌─────────────────────────────────────────────────────────────────────┐
│ Client upload file                                                   │
│ POST /api/keytools/file                                             │
│ Content-Type: multipart/form-data                                   │
│ Form Data:                                                           │
│   - File: keys.txt                                                  │
│   - Algorithm: 1 (or 2)                                             │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ KeyToolsController.ValidateFile(FileUploadRequest request)         │
│                                                                      │
│ 1. Kiểm tra file != null và có nội dung                            │
│ 2. Mở stream: request.File.OpenReadStream()                        │
│ 3. Set algorithm: _keyQualityService.SetAlgorithm(request.Algorithm)│
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ IInputSource.ExtractKeyAsync(stream)                               │
│ (Implementation: InputtxtStrategy)                                  │
│                                                                      │
│ - Đọc file text                                                     │
│ - Parse từng dòng                                                   │
│ - Trích xuất các keys (có thể lọc comment, dòng trống)             │
│ - Return List<string> keys                                          │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Kiểm tra keys.Count > 0                                             │
│ if (keys.Count == 0) → BadRequest("No keys found")                 │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ KeyQualityService.ValidateBatch(keys)                              │
│ → Xử lý tương tự như ValidateBatch ở trên                          │
│ → Sử dụng algorithm đã set từ request.Algorithm                    │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Controller trả về BatchKeyCheckResult                               │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 5. Design Patterns Được Sử Dụng

### 5.1 Strategy Pattern

**Mục đích**: Cho phép thay đổi thuật toán mã hóa mà không ảnh hưởng đến code chính.

```
ICryptoStrategy (Interface)
    └── TripleDesStrategy (Hiện tại)
    └── AesStrategy (Có thể thêm sau)
    └── DesStrategy (Có thể thêm sau)
```

**Lợi ích**:

- Dễ dàng thêm thuật toán mới (AES, DES, etc.)
- Tuân thủ Open/Closed Principle
- KeyQualityService không phụ thuộc vào implementation cụ thể

### 5.2 Dependency Injection

```csharp
public KeyQualityService(ICryptoStrategy cryptoStrategy)
{
    _cryptoStrategy = cryptoStrategy;
}
```

**Lợi ích**:

- Loose coupling
- Dễ test (có thể mock ICryptoStrategy)
- Configuration tập trung trong Program.cs

### 5.3 Extension Method Pattern

```csharp
public static string GetDescriptionOfEnum<T>(this T enumValue) where T : Enum
```

**Mục đích**: Lấy Description attribute từ enum value.

**Sử dụng**:

```csharp
Message.InvalidFormat.GetDescriptionOfEnum()
// → "Chứa ký tự không phải Hex"
```

---

## 6. Validation Rules Chi Tiết

### Quy Tắc 1: Empty Check

- **Điều kiện**: `string.IsNullOrWhiteSpace(rawKey)`
- **Kết quả**: `InvalidFormat` - "Key rổng"

### Quy Tắc 2: Hex Format

- **Điều kiện**: `!Regex.IsMatch(rawKey, "^[0-9a-fA-F]+$")`
- **Cho phép**: Chỉ các ký tự 0-9, a-f, A-F
- **Kết quả**: `InvalidFormat` - "Chứa ký tự không phải Hex"

### Quy Tắc 3: Length Check

- **Điều kiện**: `rawKey.Length != 32 && rawKey.Length != 48`
- **Cho phép**:
  - 32 ký tự hex = 16 bytes = 128 bits (2-key Triple DES)
  - 48 ký tự hex = 24 bytes = 192 bits (3-key Triple DES)
- **Kết quả**: `InvalidLength` - "Độ dài không đúng chuẩn 3DES (32/48)"

### Quy Tắc 4: Odd Parity

- **Điều kiện**: Mỗi byte phải có số lượng bit 1 là lẻ
- **Kết quả**: `BadParity` - "Sai bit chẵn lẻ (Odd Parity)"

### Quy Tắc 5: Weak Key Detection

- **Điều kiện**: Thuật toán 3DES throw exception
- **Nguyên nhân**:
  - Key có pattern đặc biệt (all 0, all 1)
  - Weak keys của DES (vd: 0x0101010101010101)
  - Semi-weak keys
- **Kết quả**: `WeakKey` - "Key yếu hoặc lỗi thuật toán"

---

## 7. Error Handling

### 7.1 Controller Level

- **Null/Empty Request**: `BadRequest("Key is required.")`
- **Empty Keys List**: `BadRequest("At least one key is required.")`
- **Empty File**: `BadRequest("File is empty.")`
- **No Keys in File**: `BadRequest("No keys found in file.")`

### 7.2 Service Level

- **Try-Catch** trong `CalculateKcv`: Bắt exception từ 3DES → WeakKey status
- Tất cả validation errors được trả về trong `KeyCheckResult`, không throw exception

### 7.3 HTTP Status Codes

- `200 OK`: Validation thành công (kể cả key không hợp lệ, vì request đã được xử lý)
- `400 Bad Request`: Request format sai hoặc thiếu data

---

## 8. Security Considerations

### 8.1 Key Handling

- ✅ Keys không được log ra console/file
- ✅ Keys chỉ tồn tại trong memory trong quá trình xử lý
- ✅ Không lưu trữ keys vào database
- ⚠️ KCV được trả về nhưng không tiết lộ key gốc

### 8.2 Input Validation

- Regex chặt chẽ cho hex format
- Length validation trước khi xử lý
- Trim whitespace để tránh lỗi

### 8.3 Cryptographic Standards

- Sử dụng `System.Security.Cryptography.TripleDES`
- ECB mode (phù hợp cho KCV calculation)
- No padding (key length cố định)

---

## 9. Testing Scenarios

### 9.1 Valid Keys

```
✅ "0123456789ABCDEFFEDCBA9876543210" (32 chars, odd parity)
✅ "112233445566778899AABBCCDDEEFF00112233445566" (48 chars)
```

### 9.2 Invalid Format

```
❌ "GHIJK12345" (contains G, H, I, J, K)
❌ "012345 ABCDEF" (contains space)
❌ "0x123456789ABCDEF" (contains 0x prefix)
```

### 9.3 Invalid Length

```
❌ "0123456789ABCDEF" (16 chars - too short)
❌ "0123456789ABCDEFFEDCBA98765432101234" (36 chars - not 32 or 48)
```

### 9.4 Bad Parity

```
❌ "0023456789ABCDEFFEDCBA9876543210" (byte 0x00 has 0 bits = even)
```

### 9.5 Weak Keys (Examples)

```
❌ "0000000000000000" (all zeros)
❌ "FFFFFFFFFFFFFFFF" (all ones)
❌ "0101010101010101" (DES weak key)
```

---

## 10. Extension Points

### 10.1 Thêm Thuật Toán Mới

1. Implement `ICryptoStrategy` interface
2. Tạo class mới (vd: `AesStrategy`)
3. Register trong DI container (Program.cs)

### 10.2 Thêm Input Source Mới

1. Implement `IInputSourceStrategy` interface
2. Tạo strategy mới (vd: `XmlFileStrategy`, `CsvFileStrategy`)
3. Configure trong controller

### 10.3 Thêm Validation Rules

- Extend `KeyValidationStatus` enum
- Extend `Message` enum
- Thêm logic check trong `ValidateKey` method

---

## 11. Performance Considerations

### 11.1 Batch Processing

- Mỗi key được validate độc lập
- Có thể parallel hóa: `Parallel.ForEach` nếu cần
- Hiện tại: Sequential processing (đơn giản, dễ debug)

### 11.2 Memory Usage

- File upload: Stream processing (không load toàn bộ vào memory)
- Keys list: Lưu tạm trong memory (hợp lý cho số lượng vừa phải)

### 11.3 Crypto Performance

- 3DES nhanh cho single block encryption
- ECB mode không cần state management
- No padding → minimal overhead

---

## 12. API Documentation Examples

### Example 1: Validate Single Key

**Request**:

```http
POST /api/keytools/key HTTP/1.1
Content-Type: application/json

{
  "key": "0123456789ABCDEFFEDCBA9876543210"
}
```

**Response** (Success):

```json
{
  "inputKey": "0123456789ABCDEFFEDCBA9876543210",
  "isValid": true,
  "status": 1,
  "message": "Key hợp lệ",
  "kcv": "8D2270"
}
```

### Example 2: Validate Batch

**Request**:

```http
POST /api/keytools/keys HTTP/1.1
Content-Type: application/json

{
  "keys": [
    "0123456789ABCDEFFEDCBA9876543210",
    "INVALID_KEY_FORMAT",
    "112233445566778899AABBCCDDEEFF00112233445566"
  ]
}
```

**Response**:

```json
{
  "totalKeys": 3,
  "validCount": 2,
  "invalidCount": 1,
  "details": [
    {
      "inputKey": "0123456789ABCDEFFEDCBA9876543210",
      "isValid": true,
      "status": 1,
      "message": "Key hợp lệ",
      "kcv": "8D2270"
    },
    {
      "inputKey": "INVALID_KEY_FORMAT",
      "isValid": false,
      "status": 2,
      "message": "Chứa ký tự không phải Hex",
      "kcv": null
    },
    {
      "inputKey": "112233445566778899AABBCCDDEEFF00112233445566",
      "isValid": true,
      "status": 1,
      "message": "Key hợp lệ",
      "kcv": "A1B2C3"
    }
  ]
}
```

---

## 13. Tóm Tắt Luồng Xử Lý

```
Request → Controller (Input validation)
        → Service (Business logic & validation rules)
        → Strategy (Crypto operations)
        → Response (Structured result)
```

**Các bước validate một key**:

1. ✅ Check null/empty
2. ✅ Check hex format (regex)
3. ✅ Check length (32 or 48 chars)
4. ✅ Check odd parity (mỗi byte có số bit 1 lẻ)
5. ✅ Calculate KCV (mã hóa block 0 bằng 3DES)
6. ✅ Detect weak keys (catch exception)

**Kết quả**: `KeyCheckResult` với status, message, và KCV (nếu hợp lệ)

---

## Glossary

- **3DES**: Triple Data Encryption Standard - Thuật toán mã hóa khối 64-bit
- **KCV**: Key Check Value - Giá trị kiểm tra key, dùng để xác minh key
- **Odd Parity**: Mỗi byte có số lượng bit 1 là số lẻ
- **ECB**: Electronic Codebook - Chế độ mã hóa đơn giản nhất
- **Weak Key**: Key có thuộc tính đặc biệt khiến mã hóa kém an toàn
- **Hex**: Hexadecimal - Hệ cơ số 16 (0-9, A-F)
- **DTO**: Data Transfer Object - Object dùng để truyền dữ liệu giữa layers

---

**Tài liệu này mô tả đầy đủ luồng xử lý từ request → response của hệ thống Payment Card Key Validation.**
