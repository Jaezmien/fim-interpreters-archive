from src.fimCore import FiMReport
import sys

while True:
    try: 
        file_input = input("Enter file name: ")
        file_name = f"examples/{file_input}.fim"

        import os.path
        if not os.path.isfile(file_name):
            print(f"{file_name} isn't a file!")
        if not file_name.endswith(".fim"):
            print(f"{file_name} isn't a .fim file!")

        with open(file_name) as fc:
            content = fc.read()
            
        report = FiMReport( content )
        print("--+ FiMPy Playground +---------------")
        print("- Report Name: " + report.Report_Name)
        print("- Reporter: " + report.Report_Writer)
        print("-+ Output +--------------------------")
        print("?-- REPORT DUMP")
        print(content.replace("\n\n","\n"))
        print("!-- REPORT RESULT")
        report.RunParagraph()
    except Exception as e:
        print(f"[ERROR] {e}")
pass
#print( report.JSONVariables() )