# স্ক্রিপ্ট থেকে KillerBench চালান

কোনো স্ক্রিপ্ট, RMM কাজ বা রিমোট কনসোলে KillerBench-এর প্রমাণ চাইলে কমান্ড লাইন ব্যবহার করুন। `killerbench-cli` ডেস্কটপ অ্যাপের মতো একই Engine চালায় এবং JSON আউটপুট দেয়।

## শুরু করুন

1. যেখানে `killerbench-cli.exe` আছে সেখানে PowerShell খুলুন, অথবা এর ফোল্ডারটি আপনার পাথে যোগ করুন।
2. প্রতিটি কমান্ড তার নিরাপত্তা সীমাসহ তালিকা করতে `killerbench-cli help` চালান।
3. 11টি প্রস্তাবিত ডায়াগনস্টিক গ্রুপ দেখতে `killerbench-cli diagnostic-scopes` চালান।

## একটি স্ক্যান চালিয়ে রিপোর্ট সংরক্ষণ করুন

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

আপনি `--auto-repair` যোগ না করলে `full-scan` শুধু-পড়া। `--report` অ্যাপ যে থিমযুক্ত রিপোর্ট রপ্তানি করে সেটিই লেখে। কেবল কিছু ক্ষেত্র পরীক্ষা করতে `--scope` ব্যবহার করুন, এবং ওয়ার্কফ্লো জার্নাল ছাড়া সম্মিলিত JSON-এর জন্য `common-check` ব্যবহার করুন।

## এক্সিট কোড পড়ুন

| কোড | অর্থ |
| --- | --- |
| 0 | প্রিভিউ বা যাচাইকৃত ফলাফল সফল হয়েছে। |
| 1 | চালু হওয়া, ইনপুট বা জার্নালের ত্রুটি কমান্ডটি থামিয়ে দিয়েছে। |
| 2 | আর্গুমেন্টগুলো চেনা যায়নি। |
| 3 | আটকে গেছে, ব্যর্থ হয়েছে বা যাচাই হয়নি। |
| 4 | যাচাইয়ের আগে একটি ওয়ার্কফ্লোর রিস্টার্ট প্রয়োজন। |
| 130 | আপনি এটি থামিয়েছেন। |

শুধু-পড়া কমান্ডের এক্সিট 0 মানে কমান্ডটি চলেছে, কম্পিউটারটি সুস্থ নয়। সিদ্ধান্তের জন্য JSON পড়ুন।

## একটি পরিবর্তন করুন

কম্পিউটার পরিবর্তন করতে পারে এমন প্রতিটি কমান্ড ডিফল্টভাবে প্রিভিউ দেখায়। প্রিভিউ পড়ুন, তারপর একই কমান্ড `--execute` দিয়ে চালান। কোনো অনুরোধ বাধাপ্রাপ্ত হলে সংশ্লিষ্ট `-recover` কমান্ড চালান; এটি আসল ফলাফল জানায় এবং পরিবর্তনটি কখনো পুনরাবৃত্তি করে না।

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## পুরোনো ফলাফল আবার খুলুন

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## একটি AI সহকারী থেকে ব্যবহার করুন

[KillerMCP](../mcp.html) Device Manager ও Windows ত্রুটি কোড খুঁজে দেখতে ইনস্টল করা CLI কল করতে পারে।

## সম্পর্কিত

- [কমান্ড লাইন রেফারেন্স](cli.html)
- [Engine ডেভেলপার নির্দেশিকা](../engine/index.html)
- [সমস্যা সমাধান](troubleshooting.html)
