# Công nghệ của KillerBench

Một cái nhìn bên trong cách KillerBench đọc máy tính Windows, giữ bằng chứng trung thực và quyết định khi nào một thay đổi được phép.

## Công nghệ sử dụng

KillerBench là ứng dụng Windows gốc. Ứng dụng máy tính, dòng lệnh và Engine nằm bên dưới chúng dùng chung bằng chứng chẩn đoán và quy trình sửa chữa.

| Thành phần | Chi tiết |
| --- | --- |
| Giao diện | WPF trên .NET 10 (`net10.0-windows`), Windows x64, khung cửa sổ tùy chỉnh |
| Engine | `Engine.Core`, `Engine` và `Engine.Windows`, không tham chiếu WPF hay điều khiển của ứng dụng máy tính |
| Dòng lệnh | `killerbench-cli`, một máy chủ riêng chạy trên cùng Engine |
| Nhà cung cấp Windows | .NET, API Windows, PowerShell và công cụ dòng lệnh Windows, mỗi thứ chạy trong giới hạn cố định về thời gian, số bản ghi và kích thước |
| Báo cáo | Đầu ra thô, Báo cáo dễ đọc, HTML, văn bản và JSON |
| Dữ liệu tham chiếu | Gói tri thức có phiên bản cho mã lỗi, mã định danh phần cứng, nhà cung cấp và tài liệu hỗ trợ |
| Đóng gói | Trình cài đặt: phụ thuộc framework, cần .NET Desktop Runtime 10 for Windows x64. Bản di động: độc lập, không cần cài môi trường chạy |
| Giao diện hiển thị | 13 chủ đề của bộ công cụ và 19 ngôn ngữ |

## Cài đặt, thư mục và dữ liệu

KillerBench có hai dạng. Trình cài đặt là bản tải xuống tiêu chuẩn và hỗ trợ cài cho một người dùng hoặc cho mọi người dùng. Bản di động mang theo môi trường chạy .NET riêng và chạy mà không cần cài đặt.

KillerBench chạy với quyền của tài khoản đã khởi động nó. Công cụ không tự nâng quyền. Hãy khởi động nó bằng **Chạy với tư cách quản trị viên** khi bằng chứng được bảo vệ, như trình bảo vệ BitLocker, hoặc một sửa chữa cần quyền quản trị viên. Việc nâng quyền không bao giờ chọn hay phê duyệt sửa chữa.

### Trình cài đặt làm gì

Trình cài đặt xác minh và giải nén ứng dụng vào `%LOCALAPPDATA%\Programs\KillerBench\`, hoặc vào `%ProgramFiles%\KillerBench\` sau lời nhắc nâng quyền khi cài cho mọi người dùng. Trình cài đặt thêm lối tắt trong menu Bắt đầu, lối tắt trên màn hình nền tùy chọn và một mục trong Thêm hoặc loại bỏ chương trình với cách gỡ cài đặt thông thường và gỡ cài đặt im lặng. Bản di động xác minh và giải nén nội dung vào `%LOCALAPPDATA%\KillerBench\Portable\` cho lần chạy hiện tại và dọn các bản sao bị bỏ lại ở lần khởi động tiếp theo.

### Nơi lưu trữ dữ liệu

| Vị trí | Nội dung |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Chủ đề, màu nhấn, tỷ lệ giao diện và ngôn ngữ |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Một nhật ký JSON cho mỗi lần chạy quy trình: quyết định, đầu ra lệnh có giới hạn và kết quả sửa chữa |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Nhật ký thực thi cho các sửa chữa bảo trì như DISM và SFC |
| Các thư mục tính năng trong `%LOCALAPPDATA%\KillerBench\` | Bản ghi chờ và kết quả của từng thao tác được bảo vệ, ví dụ `BitLockerBackup`, `Time`, `Firewall` và `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | Mức sửa chữa KillerMCP được chọn trong Giới thiệu |
| `HKCU` hoặc `HKLM\Software\KillerBench` | Đường dẫn cài đặt, theo phạm vi cài đặt tương ứng |
| Một tệp `.kbench` do bạn chọn | Một ảnh chụp đã lưu cùng bằng chứng được giữ lại |

Không có gì được tải lên. Chỉ xóa nhật ký khi bạn không còn cần bằng chứng cho một kết quả không chắc chắn.

## Chẩn đoán trước

KillerBench coi chẩn đoán và thay đổi là hai việc khác nhau. Chẩn đoán chỉ đọc. Thay đổi chỉ xảy ra khi bạn chọn một sửa chữa hoặc thao tác cụ thể, xem lại tùy chọn và điều kiện tiên quyết của nó, và xác nhận.

Quét toàn bộ hệ thống chỉ chẩn đoán, kể cả khi một quy trình chuyên biệt đang bật sửa chữa tự động. Ảnh chụp đã lưu, nhật ký được nhập và sự kiện trong quá khứ không bao giờ cho phép sửa chữa.

### Thu thập có giới hạn

Mọi lệnh gốc và nhà cung cấp Windows đều chạy trong giới hạn cố định về thời gian, số bản ghi và kích thước. Nhà cung cấp ghi lại trạng thái thoát, hết thời gian chờ, việc cắt bớt, đầu ra chuẩn và đầu ra lỗi. Một nhà cung cấp thất bại hoặc bị cắt bớt không bao giờ được hiểu là một câu trả lời sạch, trống.

### Lưu giữ tối thiểu

Bộ thu thập chỉ giữ những gì một kết luận cần. Nhiều kết quả giữ tên, danh mục, số lượng, trạng thái và dấu thời gian, đồng thời bỏ qua thông tin xác thực, dòng lệnh, đường dẫn đầy đủ, danh tính tài khoản, điểm cuối mạng và nội dung sự kiện không liên quan.

## Bằng chứng có thể lần theo

Mọi kết luận đều gắn với bằng chứng đã tạo ra nó, và báo cáo luôn tách điều đã được chứng minh khỏi điều chỉ được quan sát.

| Trạng thái | Ý nghĩa |
| --- | --- |
| Cần xem lại | Bằng chứng hỗ trợ một vấn đề đáng được chú ý. |
| Thông tin | Bằng chứng đã được thu thập và hiển thị để tham khảo ngữ cảnh. |
| Chưa hoàn tất hoặc Không rõ | Một kiểm tra không thể hoàn tất hoặc bằng chứng không khả dụng. Kết quả này không bao giờ được tính là khỏe mạnh. |
| Lịch sử | Một sự kiện trong quá khứ. Nó giải thích lịch sử nhưng không chứng minh lỗi hiện tại. |
| Đã xác minh | Một sửa chữa hoặc thay đổi đã được xác nhận bằng cách đọc lại kết quả. |

### Không rõ cũng là một kết quả

Không rõ nghĩa là quyền truy cập bị từ chối, nguồn bị thiếu, nhà cung cấp thất bại, đầu ra sai định dạng, vượt giới hạn hoặc bằng chứng mâu thuẫn. KillerBench hiển thị giới hạn đó cùng bước hữu ích tiếp theo thay vì che giấu nó.

### Lịch sử vẫn là lịch sử

Bản ghi sự kiện và nhật ký được nhập mô tả những gì đã được ghi lại tại một thời điểm trong quá khứ. Chúng có thể chỉ tới một mã lỗi hoặc phạm vi chẩn đoán tiếp theo. Chúng không chứng minh rằng lỗi vẫn còn hoạt động.

## Sửa chữa được bảo vệ

Sửa chữa là các thao tác có kiểu với quy tắc về điều kiện, thực thi, xác minh và khôi phục. Chúng không phải là chuỗi lệnh được lấy ra từ một dòng nhật ký. Mọi sửa chữa được hỗ trợ đều theo cùng một trình tự:

1. Thu thập đầy đủ bằng chứng hiện tại.
2. Khớp với một công thức được hỗ trợ.
3. Xem lại các tùy chọn bắt buộc và hệ quả.
4. Xác nhận điều kiện tiên quyết về quản trị và chính sách.
5. Thu thập lại bằng chứng quyết định ngay trước khi thay đổi.
6. Ghi một bản ghi chuẩn bị bền vững.
7. Chạy một thao tác gốc có giới hạn.
8. Thu thập bằng chứng xác minh mới.
9. Ghi lại kết quả đã xác minh, bị chặn, đã dừng hoặc không chắc chắn.

Nếu bằng chứng quyết định thay đổi trước khi thao tác chạy, KillerBench dừng lại mà không thực hiện nó.

### Lựa chọn tường minh

Các sửa chữa phụ thuộc vào một tùy chọn cần bạn chọn chính xác, như ổ đĩa, hàng đợi máy in, nơi lưu sao lưu khóa khôi phục, giá trị nguồn điện, phiên bản gói hoặc nguồn sửa chữa. KillerBench không bao giờ tự đặt ra các giá trị đó.

### Máy tính được quản lý

Chính sách nhóm và quản lý thiết bị có thể khiến một thay đổi cục bộ không có hiệu lực hoặc không phù hợp. Các sửa chữa được hỗ trợ kiểm tra ai sở hữu một cài đặt và dừng lại khi quyền sở hữu đó được quản lý hoặc không rõ. Các cài đặt do chính sách sở hữu được giữ nguyên.

### Sửa chữa tự động

Sửa chữa tự động chỉ cho phép các công thức đủ điều kiện tiếp tục. Nó không bỏ qua bằng chứng, không chọn tùy chọn còn thiếu, không phớt lờ chính sách, không thử lại một thao tác không chắc chắn, không khởi động lại Windows và không biến một kết luận cần xem lại thành sửa chữa.

## Nhật ký và gián đoạn

Trước khi yêu cầu Windows thực hiện một thay đổi, KillerBench ghi lại những gì sắp làm. Mỗi thay đổi được lưu trong một nhật ký tại `%LOCALAPPDATA%\KillerBench`.

Sau khi thay đổi, KillerBench đọc lại kết quả và chỉ báo cáo **Đã xác minh** khi Windows cho thấy trạng thái mong đợi. Nếu Windows báo thành công nhưng trạng thái không thay đổi, kết quả sẽ nói rõ và không có gì được lặp lại.

Một tiến trình có thể dừng sau khi Windows nhận yêu cầu nhưng trước khi việc xác minh hoàn tất. Bản ghi chờ được giữ lại, và một lần đối chiếu sau đó đọc trạng thái mới để xác định điều gì đã xảy ra. Đối chiếu không phải là hoàn tác tự động và không bao giờ lặp lại thay đổi ban đầu. Hãy giữ nhật ký khi kết quả không chắc chắn.

## Hằng số và giới hạn

| Thông số | Giá trị |
| --- | --- |
| Phạm vi chẩn đoán | Quét toàn bộ hệ thống chạy 49 vùng chẩn đoán và kiểm tra 96 công thức sửa chữa được bảo vệ |
| Đọc nguồn văn bản | 1.000.000 ký tự mỗi nguồn, hiển thị 2.000 dòng khớp |
| Nguồn phiên | 12 trên thanh bên; một ảnh chụp chứa tối đa 24 nguồn và 64 MB |
| Truy vấn sự kiện | 200 sự kiện mới nhất mỗi kênh trong 7 ngày gần nhất |
| Quét Windows Update | 15 giây mỗi nguồn, tổng cộng hai phút |
| Điều tra nhật ký | 10.000 mục đã phân tích, 256 mã khác nhau, 8 đoạn trích 320 ký tự cho mỗi mã |
| Kho lưu trữ CBS nén | Kho lưu trữ 256 MiB, nhật ký thành phần 512 MiB, giải nén 30 giây |
| CHKDSK chỉ đọc | Mười phút; một sửa chữa được giới hạn trong 65 phút |
| Kiểm tra DNS và TCP | Mười giây theo mặc định, tối đa một phút |
| Kiểm kê dịch vụ | Tối đa 1.024 dịch vụ đã cài |
| Gói tri thức | Tối đa 32 gói mở cùng lúc |
| Xem lại tệp lớn | Tệp từ 100 MB trở lên |
| Theo dõi hoạt động bảo trì | Khoảng thời gian 1 đến 60 giây, 1 đến 120 lần chụp |
| Bản nháp tác vụ đã lên lịch | Giới hạn thực thi từ 1 đến 4.320 phút |
| Nguồn thời gian tùy chỉnh | Tối đa tám máy NTP ngang hàng |
| Xuất hoạt động | Tối đa 150 mục được giữ lại |
| Ngôn ngữ và chủ đề | 19 ngôn ngữ, 13 chủ đề với các biến thể màu nhấn |

## Những gì công cụ sẽ không làm

- Xóa TPM, xóa trình bảo vệ mà nó không tạo ra, hoặc hiển thị mật khẩu khôi phục.
- Thay đổi cài đặt do chính sách nhóm hoặc quản lý thiết bị kiểm soát.
- Chạy sửa chữa chỉ vì một sự kiện lịch sử hoặc báo cáo được nhập có nhắc đến vấn đề.
- Biến bằng chứng bị thiếu, một phần hoặc thất bại thành kết luận khỏe mạnh.

## Báo cáo

Mỗi lần chạy tạo ra một báo cáo mà bạn có thể đọc, lọc và xuất. Báo cáo tách riêng:

1. Những gì đã được kiểm tra
2. Vấn đề nào đã được tìm thấy
3. Bằng chứng nào hỗ trợ kết luận
4. Thao tác nào đã được đề xuất hoặc thực hiện
5. Kết quả đã được xác minh như thế nào
6. Điều gì vẫn chưa rõ hoặc cần khởi động lại

**Đầu ra thô** giữ bằng chứng kỹ thuật cùng số dòng gốc để bạn có thể lần theo bất kỳ kết luận nào về nguồn của nó. **Báo cáo dễ đọc** tóm tắt cùng lần chạy đó bằng ngôn ngữ đơn giản, với bước an toàn tiếp theo đặt lên đầu. Báo cáo HTML dùng hệ thống chủ đề của Killer Tools và vẫn đọc được khi không có ứng dụng. Văn bản thuận tiện cho việc di chuyển, còn JSON giữ các trường có cấu trúc cho chương trình khác.

## Nhật ký và ảnh chụp đã lưu

KillerBench có thể điều tra bằng chứng mà không chạy gì trên máy. Thả một nhật ký văn bản vào cửa sổ, hoặc dán đường dẫn của nó và chọn **Mở nhật ký**. Các dòng CBS, CSI và Setup được phân tích cùng dấu thời gian, và kết luận liên kết đến đúng dòng gốc. Tệp EVTX và XML sự kiện có thể được nhập theo cách tương tự.

**Quét Windows Update** là việc thu thập theo yêu cầu, chỉ đọc, các nhật ký CBS, DISM, Panther và Setup hoàn nguyên, các kênh sự kiện cập nhật và bảo trì, trạng thái dịch vụ cập nhật, dung lượng trống, thời gian hoạt động, chỉ báo khởi động lại đang chờ và chính sách cập nhật được chọn. **Lưu ảnh chụp** ghi bằng chứng được giữ lại vào một tệp `.kbench`. **Mở ảnh chụp** mở lại tệp đó mà không cần các tệp gốc và đánh giá lại bằng các quy tắc đã cài, nên cùng quy tắc cho ra cùng kết luận.

## Chế độ demo

Khởi động `KillerBench.exe --demo` để thử **Chạy chẩn đoán** trên một máy trạm giả lập. Mọi quy trình đều tạo bằng chứng tổng hợp và tiến trình theo thời gian. Không có nhà cung cấp Windows nào được gọi, không có lệnh sửa chữa nào chạy và không có nhật ký nào được ghi. Các nút công cụ riêng bên ngoài **Chạy chẩn đoán** vẫn hoạt động thật, vì vậy đừng dùng chúng như thao tác demo.

`KillerBench.exe --demo-screenshot` mở một báo cáo Quét toàn bộ hệ thống chỉ để xem cho một máy giả lập cố định. Thao tác nhấp trong không gian làm việc, phím tắt và thả tệp đều bị chặn.

## Gói tri thức

Dữ liệu tham chiếu lớn và thường xuyên được cập nhật nằm trong các gói tri thức thay vì trong logic chẩn đoán: mã lỗi Windows và Setup, mã định danh nhà cung cấp và thiết bị, tham chiếu PCI, USB, ACPI và UEFI, dữ liệu nhà cung cấp OUI mạng, vị trí trình điều khiển và hỗ trợ, và các liên kết hỗ trợ được tuyển chọn.

Mỗi gói xác định định dạng, phiên bản lược đồ, nguồn, bản sửa đổi nguồn, thời điểm tạo và hàm băm nội dung, và KillerBench xác thực chúng trước khi dùng. Một gói không rõ hoặc không hợp lệ vẫn không khả dụng thay vì bị thay bằng phỏng đoán. Bản cập nhật được tải vào một vị trí tạm, được xác thực, rồi thay thế gói đang hoạt động trong một bước, nên một lần cập nhật thất bại vẫn giữ nguyên gói trước đó.

Dữ liệu tri thức có thể giải thích một mã định danh hoặc gợi ý nơi cần xem tiếp. Nó không thể biến một mã trong quá khứ thành lỗi hiện tại, tự động chọn trình điều khiển hay cho phép sửa chữa.

## Cập nhật và ký mã

Khi khởi động, KillerBench hỏi GitHub về bản phát hành ổn định mới nhất, với giới hạn bốn giây, và chỉ đề xuất cập nhật khi bản phát hành đó mới hơn. Bạn có thể tắt việc kiểm tra bằng **Kiểm tra cập nhật khi khởi động** trong Giới thiệu, hoặc trong lời nhắc cập nhật. Bản tải xuống là gói khớp với bản đang chạy, trình cài đặt hoặc bản di động, và SHA-256 của nó phải khớp với `SHA256SUMS.txt` của bản phát hành trước khi bất cứ thứ gì được cài. Ứng dụng tự đóng và mở lại để hoàn tất. Không có dịch vụ nền nào.

Thẻ Giới thiệu hiển thị phiên bản, bên phát hành và dấu vân tay của chứng chỉ ký mã, cùng SHA-256 của tệp thực thi đang chạy, để bạn có thể kiểm tra bản sao mình đang có.

## KillerMCP

KillerBench không bao gồm tác nhân AI. [KillerMCP](../mcp.html) có thể kết nối một tác nhân với cùng Engine. **Tùy chọn KillerMCP** trong Giới thiệu quyết định tác nhân đó được phép làm gì:

| Cài đặt | Tác dụng |
| --- | --- |
| Tắt (mặc định) | Tác nhân không thể yêu cầu sửa chữa. |
| Cho phép KillerMCP chạy các sửa chữa an toàn | Tác nhân có thể yêu cầu các sửa chữa được phân loại là an toàn. |
| Cho phép cả các sửa chữa gây gián đoạn | Các sửa chữa gây gián đoạn trở nên khả dụng, và mỗi sửa chữa vẫn cần phê duyệt. |

Chỉ người đang ngồi tại máy tính này mới có thể nâng mức. Sau một kết quả sửa chữa không chắc chắn, KillerMCP ghi một khóa, và các sửa chữa của tác nhân vẫn bị chặn cho đến khi có người gỡ khóa trong Giới thiệu.

## Bản địa hóa

Giao diện có sẵn bằng 19 ngôn ngữ: tiếng Anh, tiếng Bengal, tiếng Séc, tiếng Đức, tiếng Tây Ban Nha, tiếng Pháp, tiếng Hungary, tiếng Ý, tiếng Nhật, tiếng Kazakh, tiếng Na Uy, tiếng Ba Lan, tiếng Bồ Đào Nha (Brazil), tiếng Nga, tiếng Thổ Nhĩ Kỳ, tiếng Ukraina, tiếng Việt, cùng tiếng Trung giản thể và phồn thể. Việc đổi ngôn ngữ có hiệu lực ngay, bao gồm tên mục và tên quy trình, văn bản chào mừng và báo cáo dễ đọc đang mở. Văn bản còn thiếu sẽ hiển thị bằng tiếng Anh.

Bằng chứng, kết luận của Engine, nhật ký và Đầu ra thô vẫn bằng tiếng Anh, giống như nhật ký Windows, nên một báo cáo đọc như nhau trên mọi máy và có thể tìm kiếm chính xác văn bản.

## Dòng lệnh và mã thoát

`killerbench-cli` chạy cùng Engine mà không cần ứng dụng máy tính. Tiến trình được ghi ra luồng lỗi chuẩn và JSON ra đầu ra chuẩn. Phần lớn lệnh xem trước theo mặc định, và một thay đổi cần `--execute`. Các lệnh `-recover` tương ứng xem lại một thao tác bị gián đoạn mà không lặp lại nó. Xem [Chạy KillerBench từ script](use-the-cli.html) và [tham chiếu lệnh](cli.html).

| Mã thoát | Lệnh quy trình | Quét và mở |
| --- | --- | --- |
| 0 | Không có vấn đề chưa giải quyết trong các phạm vi đã kiểm tra | Hoàn tất phạm vi |
| 1 | Lỗi khởi động hoặc nhật ký | Thất bại |
| 2 | Đối số không hợp lệ | Đối số không hợp lệ |
| 3 | Cần chú ý | Thu thập một phần |
| 4 | Cần khởi động lại trước khi xác minh | |
| 130 | Đã dừng | Đã hủy hoặc hết thời gian chờ tổng thể |

Không có mã thoát nào chứng nhận rằng máy tính khỏe mạnh.

## Kiến trúc

Engine được chia thành ba dự án không có giao diện. `Engine.Core` cung cấp các thành phần cơ bản và công thức khai báo, `Engine` sở hữu các quy trình và bằng chứng dùng chung, còn `Engine.Windows` đăng ký các nhà cung cấp gốc. Ứng dụng máy tính, CLI và các bài kiểm thử là những máy chủ riêng biệt.

| Lớp | Trách nhiệm |
| --- | --- |
| Bộ thu thập năng lực | Đọc trạng thái Windows có giới hạn cho đúng một phạm vi |
| Bộ phân tích bằng chứng | Xác thực hình dạng, danh tính, độ đầy đủ và giới hạn |
| Diễn giải chẩn đoán | Chuyển bằng chứng thành các trạng thái quy trình tường minh |
| Điều phối quy trình | Kết hợp kiểm tra, tiến trình, công thức và nhật ký |
| Bộ điều phối sửa chữa | Kiểm tra lại điều kiện, chuẩn bị, thay đổi, xác minh và đối chiếu |
| Báo cáo | Trình bày vấn đề, thao tác, kết quả và giới hạn |
| Gói tri thức | Phân giải dữ liệu tham chiếu có phiên bản mà không thay đổi logic |

Bộ thu thập chặn khi không chắc chắn nếu danh tính mơ hồ, đầu ra sai định dạng, vượt giới hạn hoặc không thể kiểm tra quyền hạn cần thiết. Engine hiện nhắm tới Windows và không đa nền tảng. [Hướng dẫn dành cho nhà phát triển Engine](../engine/index.html) đề cập đến API, CLI và việc nhúng Engine vào một máy chủ .NET khác.

## Quyền riêng tư và mạng

Chẩn đoán và phân tích nhật ký chạy cục bộ, không cần dịch vụ AI hay tài khoản KillerBench. Nhật ký và bằng chứng được giữ lại nằm trong các thư mục `%LOCALAPPDATA%\KillerBench` của tài khoản cục bộ.

Các thao tác bạn chọn có thể kết nối với đích của chúng, như Windows Update, dịch vụ tải phần mềm, hoặc sao lưu khóa khôi phục lên Active Directory hay Entra ID. Việc kiểm tra cập nhật khi khởi động kết nối với GitHub, còn bản cập nhật tri thức và trình cài đặt KillerMCP kết nối với các dịch vụ đã cấu hình của chúng. Một ứng dụng khách AI dùng [KillerMCP](../mcp.html) có thể nhận kết quả tham chiếu mà nó yêu cầu.

Nhật ký thô, tệp được nhập và bằng chứng đã xuất có thể chứa tên máy tính, tài khoản, đường dẫn hoặc văn bản nguồn nhạy cảm. Hãy xem lại chúng trước khi chia sẻ, và không bao giờ đưa khóa khôi phục, thông tin xác thực hay dữ liệu khách hàng vào báo cáo công khai.

## Tình trạng phát triển

KillerBench đang được phát triển và chưa có bản tải xuống công khai. Phạm vi sửa chữa vẫn đang mở rộng, và việc xác thực sửa chữa trên máy thật vẫn đang tiến hành. Một bản dựng thành công hay một kiểm tra mô phỏng không chứng minh hành vi sửa chữa trên máy thật. Máy ảo Windows dùng một lần được sử dụng để xác thực từng thay đổi gốc trên các phiên bản Windows và trạng thái lỗi khác nhau. Xem [Cách giữ an toàn khi thay đổi](safety.html) để biết toàn bộ mô hình an toàn.

## Bảng thuật ngữ

Định nghĩa bằng ngôn ngữ đơn giản cho các thuật ngữ dùng trên trang này, theo thứ tự bảng chữ cái.

| Thuật ngữ | Ý nghĩa |
| --- | --- |
| BITS | Background Intelligent Transfer Service, dịch vụ Windows tải bản cập nhật và các tệp khác ở chế độ nền. |
| CBS | Component-Based Servicing, hệ thống con của Windows cài đặt bản cập nhật và tính năng. Nhật ký của nó là `CBS.log`. |
| DISM | Deployment Image Servicing and Management, công cụ Windows kiểm tra và sửa kho thành phần. |
| Panther | Thư mục nơi Windows Setup ghi nhật ký nâng cấp. |
| SFC | System File Checker, công cụ Windows xác minh và khôi phục các tệp hệ thống được bảo vệ. |
| TPM | Trusted Platform Module, chip bảo mật mà BitLocker dùng để bảo vệ khóa ổ đĩa. |
| Ảnh chụp | Một tệp `.kbench` đã lưu chứa bằng chứng được giữ lại, để có thể xem lại mà không cần máy tính gốc. |
| Bản ghi chờ | Bản ghi đánh dấu một thay đổi có thể đã được gửi tới Windows. Nó chặn việc lặp lại cho đến khi đối chiếu đọc được kết quả. |
| Bằng chứng | Những gì KillerBench thực sự đọc được từ Windows, được giữ cùng nguồn của nó để có thể lần theo một kết luận. |
| Chặn khi không chắc chắn | Khi bằng chứng mơ hồ, bị thiếu hoặc quá lớn, kết quả là Không rõ và không có thay đổi nào được phép. |
| Chủ sở hữu chính sách | Chính sách nhóm hoặc quản lý thiết bị kiểm soát một cài đặt. KillerBench giữ nguyên các cài đặt có chủ sở hữu. |
| Công thức | Một sửa chữa có kiểu với quy tắc riêng về điều kiện, thực thi, xác minh và khôi phục. |
| Đã xác minh | Kết quả đã được đọc lại từ Windows và khớp với điều mà thay đổi cần đạt được. |
| Đối chiếu | Đọc trạng thái mới sau một lần gián đoạn để biết điều gì đã xảy ra, mà không lặp lại thay đổi. |
| Gói tri thức | Một tệp dữ liệu tham chiếu có phiên bản, đã được xác thực, như mã lỗi hoặc mã định danh phần cứng. |
| Nâng quyền | Chạy với quyền quản trị viên sau lời nhắc của Windows. KillerBench không bao giờ tự nâng quyền. |
| Nhật ký | Một bản ghi JSON cục bộ được ghi trước một thay đổi và cập nhật sau đó, để có thể xem lại một thao tác bị gián đoạn. |
| Quy trình | Một lần chạy của công cụ: các kiểm tra nó thu thập, các công thức nó có thể dùng và báo cáo nó tạo ra. |
