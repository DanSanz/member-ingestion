# 03: job crashes if a file isnt there

ran the job with yesterday's list. one of the files had been moved by ops and the whole thing died with a FileNotFoundException and a stack trace. the other files in the list never loaded.

surely it should say that file is rejected and carry on, like it does for a bad trailer?
