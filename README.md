# sibintech
Программа в заданной папке и её подпапках ищет все файлы, подсчитывает их хэш-суммы и сохраняет эти данные в таблице БД Oracle.

Перед запуском на реальной БД следует запустить ряд SQL-скриптов в редакторе БД, описанных в файле initialize_script.sql

Для установки папки для работы, в файле app.config установите путь к желаемой папке

<appSettings>

        <add key="folderForWork" value="C:\\Program Files\\WinRAR\\" /> #папка по умолчанию

</appSettings>

Также, при запуске программы из консоли можно передать в нее параметры args - массив строк-путей к целевым папкам.

Пример запуска программы в командной строке из папки с исполняемым файлом:
```
> .\sibintec_test.exe "D:\\VIDEO\\" "D:\\My documents\\"
```
