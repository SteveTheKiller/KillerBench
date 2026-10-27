# Về KillerBench

KillerBench là bộ công cụ chẩn đoán, sửa chữa và bảo trì Windows do Steve the Killer tạo ra, thuộc [dòng sản phẩm Killer Tools](https://killertools.net). Phiên bản 1.0.0 chưa được phát hành. Ứng dụng máy tính, Engine và CLI dùng chung bằng chứng chẩn đoán và các quy trình sửa chữa được hỗ trợ.

## Tình trạng cung cấp và phạm vi

Chưa có bản tải xuống KillerBench hay gói Engine công khai nào. [Tải xuống và thiết lập](setup.html) mô tả mục tiêu Windows x64 hiện tại và yêu cầu Desktop Runtime. Việc xác thực sửa chữa trên máy thật vẫn đang tiến hành; việc có một công cụ không chứng minh rằng mọi lỗi, mọi gián đoạn hay mọi cấu hình Windows đều đã được kiểm thử.

## Mã nguồn và thông báo phân phối

KillerBench được cấp phép theo **GPLv3**. Giấy phép phân phối là [Giấy phép Công cộng GNU phiên bản 3](../license.txt). Giấy phép mô tả các quyền và điều kiện để sao chép, sửa đổi và phân phối tác phẩm thuộc phạm vi áp dụng.

[Kho mã KillerBench](https://github.com/SteveTheKiller/KillerBench) công khai hiện cung cấp thông tin dự án. Toàn bộ phần triển khai chưa được công khai ở đó. Các hướng dẫn dành cho nhà phát triển này mô tả một bản sao mã nguồn được phép; chúng không cung cấp bản tải mã nguồn công khai.

Trước khi phân phối một bản nhị phân, bên phát hành phải cung cấp Mã nguồn tương ứng theo một phương thức được [GPLv3 mục 6](https://www.gnu.org/licenses/gpl-3.0.en.html#section6) cho phép, bao gồm mã nguồn và tài liệu dựng mà giấy phép yêu cầu. Hướng dẫn cung cấp mã nguồn và các thông báo bên thứ ba đi kèm của gói công khai đầu tiên vẫn đang chờ. GPLv3 không cấp quyền phân phối lại phông chữ của bên thứ ba; quyền sử dụng phông chữ hiện tại vẫn cần được xác minh. Hãy xem lại giấy phép, hướng dẫn mã nguồn và thông báo của gói đã phát hành trước khi phân phối lại.

## Quyền riêng tư và dịch vụ bên ngoài

Chẩn đoán và phân tích nhật ký chạy cục bộ, không cần dịch vụ AI hay tài khoản KillerBench. Nhật ký và bằng chứng được giữ lại dùng các thư mục `%LOCALAPPDATA%\KillerBench` của tài khoản cục bộ. Các thao tác được chọn có thể kết nối với Windows Update, dịch vụ tải phần mềm, thư mục dịch vụ hoặc các đích được nêu tên khác. Việc kiểm tra cập nhật khi khởi động kết nối với GitHub, còn bản cập nhật tri thức và trang trình cài đặt MCP kết nối với các dịch vụ đã cấu hình của chúng. Một ứng dụng khách AI dùng MCP có thể nhận được kết quả tham chiếu được trả về.

Trang web này lưu lựa chọn chủ đề và màu nhấn trong bộ nhớ cục bộ của trình duyệt. Các hướng dẫn của trang không chạy chẩn đoán trên máy tính của bạn. Nhật ký thô, tệp được nhập và bằng chứng đã xuất có thể chứa tên máy tính, tài khoản, đường dẫn hoặc văn bản nguồn nhạy cảm. Hãy xem lại trước khi chia sẻ; không đưa khóa khôi phục, thông tin xác thực hay dữ liệu khách hàng vào báo cáo công khai.

## Hỗ trợ

Thông tin dự án có trong kho mã công khai. Để được hỗ trợ, hãy dùng thông tin liên hệ được công bố trên [trang web của tác giả](https://thekiller.net); nêu rõ phiên bản ứng dụng, bản dựng Windows, công cụ cụ thể, kết quả mong đợi và kết quả quan sát được. Xem lại và biên tập bằng chứng trước khi gửi, và dùng kênh riêng đã được phê duyệt cho thông tin khách hàng. Giữ nhật ký cho kết quả sửa chữa không chắc chắn.

## Hướng dẫn liên quan

- [Cách giữ an toàn khi thay đổi](safety.html)
- [Báo cáo và bằng chứng](reports.html)
- [Hướng dẫn dành cho nhà phát triển Engine](../engine/)
- [KillerMCP](../mcp.html)
