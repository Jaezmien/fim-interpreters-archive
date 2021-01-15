import re
import math
from enum import Enum
import src.fimGlobal as FGlobal
import src.fimClass as FClass
import src.fimCalculation as FCalc
###

# Please tell me if there's a better way to do this
def _findFirst(t: tuple, filter, default = None):
    for x in t:
        if filter(x): return x
    return default
def _has(t: tuple, filter):
    for x in t:
        if filter(x):
            return True
    return False
def _findFirstIndex(t: tuple, filter):
    for i,x in enumerate(t):
        if filter(x): return i
    return -1
def _print(string: str):
    n_string = string
    if n_string.startswith('"') and n_string.endswith('"'):
        n_string = n_string[1:-1]
    print( "[FiM] " + n_string )

def _getOrdinal(num: int) -> str:
    num_str = str( abs(num) )
    if len(num_str) > 2:
        num_str = num_str[:-2]
    if int(num_str) >= 11 and int(num_str) <= 19: return "th"
    if num_str.endswith("1"): return "st"
    if num_str.endswith("2"): return "nd"
    if num_str.endswith("3"): return "rd"
    return "th"
def _checkOrdinal(num: str) -> bool:
    if re.search(r"\w\w$", num):
        digit = int(num[:-2])
        return num == f"{digit}{_getOrdinal(digit)}"
    return False
def _isFloat(num) -> bool:
    return float(num) != int(num)

###

def RemoveInStringComment(line: str):
    new_line = ""
    is_in_comment = False
    is_in_string = False
    is_escaped = False
    char_index = 0
    for char in line:
        next_char = '' if char_index >= len(line)-1 else line[char_index+1]
        char_index += 1
        if char == ")" and is_in_comment:
            is_in_comment = False
            continue
        
        if is_in_comment:
            continue
        
        if char == '\\':
            is_escaped = not is_escaped
            new_line += char
        elif char == "(" and not is_in_string:
            is_in_comment = True
        elif char == '"':
            if not is_escaped:
                is_in_string = not is_in_string
            new_line += char
        else:
            if char == ' ':
                if next_char != '(':
                    new_line += char
            else:
                new_line += char

    return new_line

def IsLineEmpty(line: str) -> bool:
    if ( not line.strip() or re.search(r"^\s*(P\.)(P\.)*S\. ", line) ):
        return True
    #if not re.sub( r"(\([^\)]*\))", "", line ).strip():
    #    return True
    return False
def RemoveComment(line: str) -> str:
    #new_line = re.sub( r"(\([^\)]*\))", "", line ) # Remove parentheses
    new_line = re.sub( r"(?<=\.|\!|\?|\:|\,) (P\.)(P\.)*S\. .*", "", line ) # Remove end post script
    return new_line
def StripEndPunctuation(line: str) -> str:
    return line[:-1] if line.endswith( FGlobal.Punctuations ) else line

def RemoveIndent(line: str, indent_amount: int = 1) -> str:
    new_line = line
    for _ in range(indent_amount):
        if not re.match(r"\s{4}", new_line):
            return None
        new_line = new_line[4:]
    return new_line if new_line == new_line.strip() else None

def IsValidVariableName(name: str):
    if name.startswith(" ") or name.endswith(" "):
        return False
    if re.match(r"\d", name):
        return False
    if _has(FGlobal.Keywords, lambda x: ((f" {x}" in name) or (f"{x} " in name))):
        return False
    return True

###

def IsNull(value):
    if value == '""': return True
    if value == 0: return True
    if value == False: return True
    if value == "''": return True
    return False

def ConvertBooleanString(string: str) -> bool:
    if string in FGlobal.Methods["Boolean True"]: return True
    if string in FGlobal.Methods["Boolean False"]: return False
    raise Exception(f"Cannot convert {string} to boolean")
    

def SanitizeString(string: str, variables: dict, report) -> str:
    new_string = ""
    buffer = ""
    is_in_string = False

    def buffer_check(new_string, buffer, isEnd):
        if buffer.strip(): # We have something in the buffer.
            buffer = buffer.strip()
            var_value,_ = BlindConvertStringToProperValue(buffer, variables, report, isEnd)
            if str( var_value ).startswith('"') and str( var_value ).endswith('"'):
                return str( var_value )[1:-1]
            if str( var_value ).startswith("'") and str( var_value ).endswith("'"):
                return str( var_value )[1:-1]
            return str( var_value )
        else: # Empty buffer
            return ""

    is_escaped = False
    for char in string:
        if char == '"' and not is_escaped:
            if not is_in_string:
                new_string += buffer_check(new_string, buffer, False)
            else:
                new_string += buffer
            buffer = ""
            is_in_string = not is_in_string
        elif char == "\\" and not is_escaped:
            is_escaped = True
        else:
            if is_escaped:
                if char == "n":
                    buffer += "\n"
                else:
                    buffer += "\\" + char
                is_escaped = False
            else:
                buffer += char

    new_string += buffer_check(new_string, buffer, True)

    return f'"{new_string}"'
    #return new_string

###

def GrabVariableInitializerType(line: str) -> (FClass.FiMVariableTypes,int):
    """
        Grabs (the number, the string, etc.) and returns its FimVariableType and length
    """
    # Boolean
    if line.startswith( FGlobal.Methods['Variable Boolean Array'] ):
        length = len( _findFirst(FGlobal.Methods['Variable Boolean Array'], lambda x: line.startswith(x)) )
        return FClass.FiMVariableTypes.BOOL_ARRAY, length
    if line.startswith( FGlobal.Methods['Variable Boolean'] ):
        length = len( _findFirst(FGlobal.Methods['Variable Boolean'], lambda x: line.startswith(x)) )
        return FClass.FiMVariableTypes.BOOL, length
    # Number
    if line.startswith( FGlobal.Methods['Variable Number Array'] ):
        length = len( _findFirst(FGlobal.Methods['Variable Number Array'], lambda x: line.startswith(x)) )
        return FClass.FiMVariableTypes.NUMBER_ARRAY, length
    if line.startswith( FGlobal.Methods['Variable Number'] ):
        length = len( _findFirst(FGlobal.Methods['Variable Number'], lambda x: line.startswith(x)) )
        return FClass.FiMVariableTypes.NUMBER, length
    # String
    if line.startswith( FGlobal.Methods['Variable String Array'] ):
        length = len( _findFirst(FGlobal.Methods['Variable String Array'], lambda x: line.startswith(x)) )
        return FClass.FiMVariableTypes.STRING_ARRAY, length
    if line.startswith( FGlobal.Methods['Variable String'] ):
        length = len( _findFirst(FGlobal.Methods['Variable String'], lambda x: line.startswith(x)) )
        return FClass.FiMVariableTypes.STRING, length
    # Char
    if line.startswith( FGlobal.Methods['Variable Character'] ):
        length = len( _findFirst(FGlobal.Methods['Variable Character'], lambda x: line.startswith(x)) )
        return FClass.FiMVariableTypes.CHAR, length

    # Default value
    return FClass.FiMVariableTypes.UNKNOWN,-1

def ConvertUnknownStringToProperValue(value: str, vtype: FClass.FiMVariableTypes):
    """
        Sanitizes known-type string to the proper value
    """
    if value == "nothing":
        no_value = {
            FClass.FiMVariableTypes.BOOL: False,
            FClass.FiMVariableTypes.NUMBER: 0,
            FClass.FiMVariableTypes.STRING: "\"\"",
            FClass.FiMVariableTypes.CHAR: "'?'"
        }
        if vtype in no_value: return no_value[ vtype ]
    
    if vtype == FClass.FiMVariableTypes.CHAR:
        if not value.startswith("'") and not value.endswith("'") and len(value[1:-1]) != 1:
            raise Exception(f"Invalid char {value}")
        return value
    if vtype == FClass.FiMVariableTypes.STRING:
        if not value.startswith('"') and not value.endswith('"'):
            raise Exception(f"Invalid string {value}")
        return value
    if vtype == FClass.FiMVariableTypes.NUMBER:
        try:
            return float(value)
        except:
            raise Exception(f"Invalid number {value}")
    if vtype == FClass.FiMVariableTypes.BOOL:
        try:
            return ConvertBooleanString( value )
        except:
            raise Exception(f"Invalid boolean {value}")

def BlindConvertStringToProperValue(value: str, variables: dict, report, run_once=False, allowPrefixArithmetic=False) -> (object, FClass.FiMVariableTypes):
    """
        Returns the variable value and its guessed type.
        Only allow prefix artihmetic if value doesn't contain `and`
    """
    if value == "nothing":
        return 0, FClass.FiMVariableTypes.NUMBER

    # Char
    if value.startswith("'") and value.endswith("'") and len(value[1:-1]) == 1:
        return value, FClass.FiMVariableTypes.CHAR
        #return value[1], FClass.FiMVariableTypes.CHAR
    # String
    if value.startswith('"') and value.endswith('"'):
        return value, FClass.FiMVariableTypes.STRING
    # Number
    try:
        if _isFloat(value):
            return float(value), FClass.FiMVariableTypes.NUMBER
        return int(value), FClass.FiMVariableTypes.NUMBER
    except:
        pass
    # Boolean
    try:
        return ConvertBooleanString( value ), FClass.FiMVariableTypes.BOOL
    except:
        pass
    # Variable Value
    if value in variables:
        #if variables[value]._isArray(True):
        #    raise Exception("Cannot convert value from array")
        return variables[value].getv(), variables[value].Type
    # Array length
    if value.startswith("length of "):
        var_name = value[len("length of "):]
        if not var_name in variables:
            raise Exception(f"Cannot find variable {var_name}")
        if not variables[var_name]._isArray():
            raise Exception("Cannot get length of non-array variable")
        return len(variables[var_name].getsv()), FClass.FiMVariableTypes.NUMBER
    # Ascii number to character
    if value.startswith("raw char of "):
        var_name = value[len("raw char of "):]
        var_value, guessed_type = BlindConvertStringToProperValue(var_name, variables, report, True)
        if guessed_type != FClass.FiMVariableTypes.NUMBER:
            raise Exception("Cannot get ASCII value of a non-number value")
        return chr(var_value), FClass.FiMVariableTypes.CHAR
    # Character to ascii number
    if value.startswith("raw num of "):
        var_name = value[len("raw num of "):]
        var_value, guessed_type = BlindConvertStringToProperValue(var_name, variables, report, True)
        if guessed_type != FClass.FiMVariableTypes.CHAR:
            raise Exception("Cannot get ASCII number of a non-char value")
        if var_value.startswith("'") and var_value.endswith("'"):
            var_value = var_value[1]
        return ord(var_value), FClass.FiMVariableTypes.NUMBER
    # Number to string
    if value.startswith("string of "):
        var_name = value[len("string of "):]
        var_value, guessed_type = BlindConvertStringToProperValue(var_name, variables, report, True)
        if guessed_type != FClass.FiMVariableTypes.NUMBER:
            raise Exception("Cannot convert a non-number value to string")
        return f'"{var_value}"', FClass.FiMVariableTypes.STRING
    # String/Bool/Char to number
    if value.startswith("number of "):
        var_name = value[len("number of "):]
        var_value, guessed_type = BlindConvertStringToProperValue(var_name, variables, report, True)
        if guessed_type == FClass.FiMVariableTypes.STRING or guessed_type == FClass.FiMVariableTypes.CHAR:
            var_value = var_value[1:-1]
            if _isFloat(var_value):
                return float(var_value), FClass.FiMVariableTypes.NUMBER    
            return int(var_value), FClass.FiMVariableTypes.NUMBER
        elif guessed_type == FClass.FiMVariableTypes.BOOL:
            return 1 if var_value else 0, FClass.FiMVariableTypes.NUMBER
        else:
            raise Exception("Cannot convert non-string or non-bool value to number")
    # Square root
    if value.startswith( FGlobal.Methods["Variable Square Root"] ):
        sqrt_prefix = _findFirst(FGlobal.Methods["Variable Square Root"], lambda x: value.startswith(x))
        var_name = value[len(sqrt_prefix):]
        var_value, guessed_type = BlindConvertStringToProperValue(var_name, variables, report, True)
        if guessed_type != FClass.FiMVariableTypes.NUMBER:
            raise Exception("Cannot square root a non-number value")
        return math.sqrt(var_value), FClass.FiMVariableTypes.NUMBER
    # Paragraph return 1
    if value in report.Paragraphs:
        if report.Paragraphs[ value ].Return_Type is None:
            raise Exception("Paragraph doesn't return anything")
        return report.RunParagraph(value).getv(), report.Paragraphs[ value ].Return_Type
    # Paragraph return 2
    if value.split(' using ')[0] in report.Paragraphs:
        paragraph_name = value.split(' using ')[0]
        if report.Paragraphs[ paragraph_name ].Return_Type is None:
            raise Exception("Paragraph doesn't return anything")
        paragraph_param = ' using '.join( value.split(' using ')[1:] )
        params = list()
        for param_index, param in enumerate( paragraph_param.split(' and ') ):
            if param_index >= len(report.Paragraphs[ paragraph_name ].Parameters): break 
            param_type,param_len = GrabVariableInitializerType(param)
            if param_type!=FClass.FiMVariableTypes.UNKNOWN:
                param = param[param_len:].strip()
            param_value, guessed_type = BlindConvertStringToProperValue(param, variables, report, run_once=True)
            if guessed_type != report.Paragraphs[ paragraph_name ].Parameters[ param_index ].Type:
                raise Exception("Invalid variable type")
            params.append(param_value)
        return report.RunParagraph(paragraph_name, params).getv(), report.Paragraphs[ paragraph_name ].Return_Type
    # Variable array value 1 and 2
    if (re.sub(r" (\d+)$", "", value) in variables) or (" of " in value):
        var_name, var_index = GetVariableArrayDetails(value, variables, report)
        if var_name is None and var_index is None:
            raise Exception("Invalid variable array initializer")
        return variables[var_name].getav(var_index), FClass.FiMVariableTypes.GetArrayType( variables[var_name].Type )
    # Arithmetic
    arith_check, arith_value = FCalc.FiMArithmetic.IsArithmetic( value )
    if arith_check:
        if (allowPrefixArithmetic) or (not allowPrefixArithmetic and not arith_value.is_prefix):
            arith = FCalc.FiMArithmetic(value, arith_value)
            arith.GetValues(variables, report)
            return arith.Calculate(), FClass.FiMVariableTypes.NUMBER

    # Return
    if run_once:
        raise Exception(f"Cannot blind convert value {value}")
    else:
        return SanitizeString(value, variables, report), FClass.FiMVariableTypes.STRING

def InterpretVariableDeclaration(line: str, variables: dict, report) -> (str,FClass.FiMVariable):
    """
        Parses [(the VAR_TYPE) VAR_NAME] lines
        Returns the string and the type
    """
    line_split = line.split(' ')
    
    variable_name: str = ""
    variable_type: FClass.FiMVariableTypes = FClass.FiMVariableTypes.UNKNOWN
    variable_value = None
    variable_constant = False

    # Grab variable name
    for index in range(0, len(line_split)):
        if line_split[index] in FGlobal.Methods['Variable Initialization']:
            variable_name = " ".join( line_split[:index] )
            line = line[ len(" ".join( line_split[:index+1] )): ]
            if line.startswith(" "): line = line[1:]
            line_split = line_split[index+1:]
            break
    if not IsValidVariableName( variable_name ):
        raise Exception(f"Invalid variable name {variable_name}")
    if variable_name in variables:
        raise Exception(f"Variable {variable_name} already exists")
    
    # (Optional) Is variable constant?
    if line.startswith("always "):
        line = line[ len("always "): ]
        line_split.pop( 0 )
        variable_constant = True
    
    # Get variable type
    vt, vl = GrabVariableInitializerType( line )
    if vt == FClass.FiMVariableTypes.UNKNOWN:
        arith_check, _ = FCalc.FiMArithmetic.IsArithmetic( line )
        if not arith_check:
            raise Exception(f"Cannot determine type of {variable_name}")
        vl = -1
        vt = FClass.FiMVariableTypes.NUMBER
    line = line[ vl+1: ]
    line_split = line.split(' ')
    variable_type = vt

    # Get variable value
    if FClass.FiMVariableTypes.IsArray(variable_type, ignore_string=True):
        if variable_constant: raise Exception("Cannot have a constant array")
        variable_value = dict()
        if line.strip():
            if len( line.split(' and ') )==1:
                if line != "nothing":
                    var_value, guessed_type = BlindConvertStringToProperValue( line, variables, report )
                    if guessed_type != variable_type:
                        raise Exception("Invalid initialized variable type")
                    variable_value = var_value
            else:   
                expected_type = FClass.FiMVariableTypes.GetArrayType(variable_type)
                array_index = 0
                for r_var in line.split(' and '):
                    if r_var == "nothing":
                        var_value = ConvertUnknownStringToProperValue( r_var, variable_type )
                    else:
                        var_value, guessed_type = BlindConvertStringToProperValue( r_var, variables, report )
                        if guessed_type != expected_type:
                            raise Exception("Invalid initialized variable type")
                    variable_value[ array_index ] = var_value
                    array_index += 1
    else:
        if line == "nothing":
            variable_value = ConvertUnknownStringToProperValue( line, variable_type )
        else:
            if variable_type == FClass.FiMVariableTypes.STRING:
                line = SanitizeString(line, variables, report)
            variable_value, guessed_type = BlindConvertStringToProperValue( line, variables, report, allowPrefixArithmetic=True )
            if guessed_type != variable_type:
                raise Exception("Invalid variable type")

    variable = FClass.FiMVariable( variable_type, variable_constant ) 
    variable.setv( variable_value )

    return variable_name, variable

###

def GetVariableArrayDetails(line: str, variables: dict, report) -> (str, int):  
    """
        Parse array variable line.
        Returns the variable name and 0-based index
    """

    if re.sub(r" (\d+)$", "", line ) in variables:
        var_name = re.sub(r" (\d+)$", "", line )
        if not var_name in variables:
            raise Exception(f"Couldn't find variable {var_name}")
        if not variables[ var_name ]._isArray():
            raise Exception(f"Variable {var_name} isn't an array")
        try:
            var_index = int( re.search(r" (\d+$)", line).group(0).strip() )
        except:
            raise Exception("Invalid array index")
        return var_name, var_index-1
    elif " of " in line:
        line_split = line.split(' of ')
        var_name_index = line_split[0].strip()
        var_name = line_split[1].strip()

        if not var_name in variables:
            raise Exception(f"Couldn't find variable {var_name}")
        if not variables[ var_name ]._isArray():
            raise Exception(f"Variable {var_name} isn't an array")

        arith_check, arith_value = FCalc.FiMArithmetic.IsArithmetic( var_name_index )
        if arith_check:
            if not arith_value.is_prefix:
                arith = FCalc.FiMArithmetic(var_name_index, arith_value)
                arith.GetValues(variables, report)
                var_index = arith.Calculate()
        else:
            if not var_name_index in variables:
                raise Exception(f"Couldn't find variable {var_name_index}")
            if variables[ var_name_index ].Type != FClass.FiMVariableTypes.NUMBER:
                raise Exception(f"Variable {var_name_index} isn't a number")
            if _isFloat( variables[ var_name_index ].getv() ):
                raise Exception(f"Cannot use floating-point numbers as array index")
            var_index = variables[ var_name_index ].getv()
        return var_name, var_index-1

    return None, None

###

def ScanMethod(report, line: str, variables: dict, r_line:str) -> FClass.FiMMethodReturn:

    # Return
    if line.startswith( "Then you get " ):
        line = line[len("Then you get "):]

        ret_type,ret_len = GrabVariableInitializerType(line)
        if ret_type != FClass.FiMVariableTypes.UNKNOWN:
            line = line[ret_len:].strip()

        args = {}
        var_value, guessed_type = BlindConvertStringToProperValue(line, variables, report)
        ret_var = FClass.FiMVariable(guessed_type)
        ret_var.setv( var_value )
        args["Return Variable"] = ret_var

        return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.RETURN, args )

    # Variable Assignment
    if line.startswith( FGlobal.Variable_Declaration ):
        print_prefix = FGlobal.Variable_Declaration
        line = line.replace(print_prefix, "", 1)
        
        args = {}
        vn,v = InterpretVariableDeclaration( line, variables, report )
        args["Variable Name"] = vn
        args["Variable"] = v

        return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.VARIABLE_INIT, args )

    # Print
    if line.startswith( FGlobal.Methods['Write'] ):
        print_prefix = _findFirst( FGlobal.Methods['Write'] , lambda x: line.startswith(x))
        line = line.replace(print_prefix, "", 1)

        # Run Function
        if [x for x in ('I remembered ','I would ') if print_prefix == x]:
            
            args = {}

            if "using" in line:
                line_split = line.split(' ')
                param_index = _findFirstIndex(line_split, lambda x: x == "using")
                param_str = " ".join( line_split[param_index+1:] )
                line = " ".join( line_split[:param_index] )

                if not line in report.Paragraphs:
                    raise Exception("Tried to run a non-existent paragraph")
                
                params = list()
                for index, param in enumerate( param_str.split(' and ') ):
                    if index >= len(report.Paragraphs[line].Parameters): break
                    param_value, guessed_type = BlindConvertStringToProperValue(param, variables, report)
                    if report.Paragraphs[line].Parameters[index].Type != guessed_type:
                        raise Exception("Invalid parameter value, unexpected type")
                    params.append(param_value)

                args["Parameter"] = params
            else:
                if not line in report.Paragraphs:
                    raise Exception("Tried to run a non-existent paragraph")            

            args["Paragraph Name"] = line

            return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.RUN, args )

        # Write
        else:
            
            # Wow I did NOT expect SanitizeString to automatically handle all of these for me
            # I dont trust my code enough
            _print( SanitizeString(line, variables, report) )

            return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.PRINT )

    # Read
    if line.startswith( FGlobal.Methods['Read'] ):
        read_prefix = _findFirst( FGlobal.Methods['Read'] , lambda x: line.startswith(x))
        line = line.replace(read_prefix, "", 1)

        if not line in variables:
            raise Exception(f"Invalid variable {line}")
        read_var: FClass.FiMVariable = variables[line]
        if read_var._isArray(ignore_string=True):
            raise Exception(f"Cannot modify an array variable")
        expected_type = read_var.Type

        user_input = input('[FiM Input]: ')
        if expected_type == FClass.FiMVariableTypes.STRING:
            user_input = f'"{user_input}"'
        elif expected_type == FClass.FiMVariableTypes.CHAR:
            if len(user_input) != 1:
                raise Exception(f"Invalid char '{user_input}'")
            user_input = f"'{user_input}'"

        args = {}
        args["Variable Name"] = line
        args["Variable Value"] = ConvertUnknownStringToProperValue(user_input, expected_type)
        
        return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.READ, args )

    # Variable Replace
    if _has( FGlobal.Methods["Variable Replace"], lambda x: f" {x} " in line):
        replace_prefix = _findFirst( FGlobal.Methods['Variable Replace'] , lambda x: f" {x} " in line)

        line_split = line.split(replace_prefix)
        var_name = line_split[0].strip()
        if not var_name in variables:
            raise Exception(f"Cannot find variable {var_name}")

        var_value = line_split[1].strip()
        # Optional
        var_value_init,var_value_initl = GrabVariableInitializerType( var_value )
        if var_value_init != FClass.FiMVariableTypes.UNKNOWN:
            var_value = var_value[var_value_initl:].strip()

        args = {}
        args["Variable Name"] = var_name
        guessed_value, guessed_type = BlindConvertStringToProperValue(var_value, variables, report, allowPrefixArithmetic=True)
        if variables[var_name].Type == FClass.FiMVariableTypes.STRING and not FClass.FiMVariableTypes.IsArray( variables[var_name].Type, True ):
            guessed_value = str( guessed_value )
        elif guessed_type != variables[var_name].Type:
            raise Exception("Replacement value isn't same type")
        if variables[var_name]._isArray(True) and not FClass.FiMVariableTypes.IsArray( variables[var_name].Type, True ):
            raise Exception(f"Cannot replace array with a non-array value")
        args["Variable Value"] = guessed_value

        return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.VARIABLE_REPLACE, args )

    # Variable Increment
    if line.startswith( "There was one more " ) or line.endswith( " got one more" ):
        if line.startswith( "There was one more " ):
            line = re.sub(r"^(There was one more )", "", line)
        else:
            line = re.sub(r"( got one more)$", "", line)
        if line in variables:
            if variables[line].Type != FClass.FiMVariableTypes.NUMBER:
                raise Exception(f"Tried incrementing a non-number variable")
            variables[line].setv( variables[line].getv() + 1 )
        else:
            var_name, var_index = GetVariableArrayDetails(line, variables, report)
            if var_name is None and var_index is None:
                raise Exception(f"Invalid variable {line}")
            if variables[var_name].Type != FClass.FiMVariableTypes.NUMBER_ARRAY:
                raise Exception("Tried incrementing a non-number array variable")
            variables[var_name].setav(var_index, variables[var_name].getav(var_index)+1)
        return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.VARIABLE_INCREMENT )

    # Variable Decrement
    if line.startswith( "There was one less " ) or line.endswith( " got one less" ):
        if line.startswith( "There was one less " ):
            line = re.sub(r"^(There was one less )", "", line)
        else:
            line = re.sub(r"( got one less)$", "", line)
        if line in variables:
            if variables[line].Type != FClass.FiMVariableTypes.NUMBER:
                raise Exception(f"Tried decrementing a non-number variable")
            variables[line].setv( variables[line].getv() - 1 )
        else:
            var_name, var_index = GetVariableArrayDetails(line, variables, report)
            if var_name is None and var_index is None:
                raise Exception(f"Invalid variable {line}")
            if variables[var_name].Type != FClass.FiMVariableTypes.NUMBER_ARRAY:
                raise Exception("Tried incrementing a non-number array variable")
            variables[var_name].setav(var_index, variables[var_name].getav(var_index)-1)
        return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.VARIABLE_DECREMENT )

        # Array modification      

    # While loop:
    if line.startswith( FGlobal.Methods["Loop While"] ):
        if line.endswith("then"):
            raise Exception("Unexpected then in while loop statement")
        return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.WHILE )
      
    # If-Else statement
    if line.startswith( FGlobal.Methods["If Statement"] ):
        # We can only calculate these in FiMCore
        return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.IF_ELSE )

    # Switch statement
    if line.startswith( "In regards to " ):
        var_name = line[len("In regards to "):]
        if not var_name in variables:
            raise Exception(f"Invalid variable {var_name}")
        if variables[var_name].Type != FClass.FiMVariableTypes.NUMBER:
            raise Exception(f"Cannot do a switch statement on a non-number variable")
        args = {}
        args["Variable Value"] = variables[var_name].getv()
        return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.SWITCH, args )

    # For loop
    if line.startswith( "For every " ):
        if not r_line.endswith("..."):
            raise Exception("Expected elipses at end")
        line = line[len("For every "):]
        line = line[:len(line)-2]
        
        var_type,type_length = GrabVariableInitializerType(line)
        line = line[type_length+1:]

        args = {}
        ret_type = FClass.FiMMethodReturnTypes.UNKNOWN

        if " from " in line and " to " in line:
            if var_type != FClass.FiMVariableTypes.NUMBER:
                raise Exception("Expected type number in for loop")
            from_split = line.split(' from ')
            var_name = from_split[0]
            to_split = from_split[1].split(' to ')
            range_min,min_type = BlindConvertStringToProperValue(to_split[0], variables, report)
            range_max,max_type = BlindConvertStringToProperValue(to_split[1], variables, report)

            if min_type != FClass.FiMVariableTypes.NUMBER or _isFloat(range_min):
                raise Exception("Expected minimum range to be int")
            if max_type != FClass.FiMVariableTypes.NUMBER or _isFloat(range_max):
                raise Exception("Expected maximum range to be int")
            if range_max-range_min < 0:
                raise Exception("Interval is empty")

            args["Minimum Value"] = int( range_min )
            args["Maximum Value"] = int( range_max )

            ret_type = FClass.FiMMethodReturnTypes.FOR_COUNT
        elif " in " in line:
            in_split = line.split(' in ')
            var_name = in_split[0]
            var_iter = in_split[1]
            if not var_iter in variables:
                raise Exception(f"Couldn't find variable {var_iter}")
            if not variables[var_iter]._isArray():
                raise Exception(f"Can't iterate through non-array variable")
            args["Iterable"] = variables[var_iter]
            ret_type = FClass.FiMMethodReturnTypes.FOR_ITER
        else:
            raise Exception("Invalid for loop")

        if var_name in variables:
            raise Exception(f"Variable {var_name} already exists")
        args["Variable Name"] = var_name
        
        return FClass.FiMMethodReturn( ret_type, args )

    # Breakpoint debug
    if line == "fucking stop you dink program":
        return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.D_BREAKPOINT )

    # Array modification
    # This is important to be checked last because of how vague the initial check is
    if _has( FGlobal.Methods["Variable Initialization"], lambda x: f" {x} " in line ):
        array_modif_prefix = _findFirst( FGlobal.Methods["Variable Initialization"], lambda x: f" {x} " in line )
        array_modif_list = line.split(f' {array_modif_prefix} ')
        array_modif_var = array_modif_list[0].strip()
        
        var_name, var_index = GetVariableArrayDetails(array_modif_var, variables, report)
        if var_name != None and var_index != None:
            var = variables[ var_name ]
            args = {}
            
            var_value = line[len(array_modif_var)+1+len(array_modif_prefix):].strip()
            # Optional
            var_value_init,var_value_initl = GrabVariableInitializerType( var_value )
            if var_value_init != FClass.FiMVariableTypes.UNKNOWN:
                var_value = var_value[var_value_initl:].strip()
            array_expected_type = None
            if var.Type == FClass.FiMVariableTypes.NUMBER_ARRAY:
                array_expected_type = FClass.FiMVariableTypes.NUMBER
            elif var.Type == FClass.FiMVariableTypes.BOOL_ARRAY:
                array_expected_type = FClass.FiMVariableTypes.BOOL
            elif var.Type == FClass.FiMVariableTypes.STRING_ARRAY:
                array_expected_type = FClass.FiMVariableTypes.STRING
            if var_value in variables:
                t_var = variables[ var_value ]
                if t_var.Type != array_expected_type:
                    raise Exception("Array unxpected variable type")
                var_value = t_var.getv()
                args["Variable Value"] = var_value
            else:
                # var_value = ConvertUnknownStringToProperValue( var_value, array_expected_type )
                if var_value == "nothing":
                    args["Delete Variable"] = True
                else:
                    var_value, guessed_type = BlindConvertStringToProperValue( var_value, variables, report, allowPrefixArithmetic=True )
                    if array_expected_type != guessed_type:
                        raise Exception("Array unxpected variable type")
                    args["Variable Value"] = var_value
            args["Slot"] = var_index
            args["Variable Name"] = var_name
            return FClass.FiMMethodReturn( FClass.FiMMethodReturnTypes.ARRAY_MODIFY, args )
      

    raise Exception(f"Invalid line {line}")