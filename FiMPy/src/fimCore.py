# Jaezmien's FiM++ Interpreter on Python 3
# friendship ended with C#, Python my new bae uwu

import src.fimClass as FClass
import src.fimMethods as FMethods
import src.fimGlobal as FGlobal
import src.fimCalculation as FCalculation

import re
import json

### Create a report class
class FiMReport:
    Variables = dict()
    Paragraphs = dict()

    def JSONVariables(self):
        var_dict = dict()
        for k,v in self.Variables.items():
            var_dict[k] = v.getv()
        return json.dumps( var_dict, indent=4 )


    def __init__(self,report_contents: str):
        self.__parse_content( report_contents )        
    
    def __parse_content(self, content: str):
        self._report_array = content.split('\n') # The content in an array form

        # Check 1: Punctuations
        for index in range(0, len(self._report_array)):
            line = FMethods.RemoveInStringComment( self._report_array[index] )
            if not FMethods.IsLineEmpty(line) and not (FMethods.RemoveComment(line)).endswith(FGlobal.Punctuations):
                raise Exception(f"Missing punctuation at line {index+1}")
            self._report_array[index] = line

        # Check 2: Report Identification
        if not self._report_array[0].startswith( FGlobal.Report_Identifiers['Start'] ):
            raise Exception("Invalid beginning of report")
        self.Report_Name = FMethods.RemoveComment(
                FMethods.StripEndPunctuation( self._report_array[0][len(FGlobal.Report_Identifiers['Start']):] )
        )
        end_index = self._report_array.index( next((x for x in self._report_array if x.startswith( FGlobal.Report_Identifiers['End'] )),None) )
        if end_index is None:
            raise Exception("Invalid end of report")
        self.Report_Writer = FMethods.RemoveComment( 
            FMethods.StripEndPunctuation( self._report_array[ end_index ][ len(FGlobal.Report_Identifiers['End']): ] )
        )
        for index in range(end_index+1,len(self._report_array)):
            if not FMethods.IsLineEmpty(self._report_array[index]):
                raise Exception(f"Expected end of report at line {index+1}")

        # Check 3: Grab all paragraph starts
        self.Variables = dict()

        self.Paragraphs = dict()
        is_in_paragraph = False
        paragraph_name = None
        paragraph_index = -1
        paragraph_main = False

        paragraph_parameters = None
        paragraph_return = None

        for index in range(1, end_index-1):
            line = self._report_array[ index ]
            if re.match("(Today )?I learned ", line):
                if is_in_paragraph:
                    raise Exception(f"Unexpected paragraph start at line {index+1}")
                if not line.strip(paragraph_name):
                    raise Exception(f"Invalid paragraph name at line {index+1}")
                is_in_paragraph = True
                paragraph_index = index+1
                
                if line.startswith("Today "):
                    if FMethods._has(self.Paragraphs.values(), lambda x: x.isMain):
                        raise Exception("More than two main paragraphs found")
                paragraph_main = line.startswith("Today ")

                paragraph_name = FMethods.RemoveComment( line.replace("Today ","")[ len("I learned "): ] )
                paragraph_name = FMethods.StripEndPunctuation( paragraph_name )

                # Method Returning
                if FMethods._has( FGlobal.Methods["Method Return"], lambda x: x in paragraph_name):
                    ret_str = FMethods._findFirst( FGlobal.Methods["Method Return"], lambda x: x in paragraph_name )
                    ret_split = paragraph_name.split( ret_str )
                    ret_type = ret_split[1].strip()

                    paragraph_return,ret_varl = FMethods.GrabVariableInitializerType(ret_type)
                    ret_type = ret_type[ret_varl:]

                    ret_split[0] = ret_split[0].strip()
                    ret_split[1] = ret_type.strip()
                    paragraph_name = " ".join(ret_split).strip()
                # Method Parameter
                try:
                    if "using" in paragraph_name:
                        paragraph_parameters = list()
                        name_split = paragraph_name.split(' ')
                        param_index = FMethods._findFirstIndex(name_split, lambda x: x == "using")
                        param_str = " ".join( name_split[param_index+1:] )

                        for param in param_str.split(' and '):
                            param_type,init_len = FMethods.GrabVariableInitializerType(param)
                            if param_type==FClass.FiMVariableTypes.UNKNOWN:
                                raise Exception('Invalid parameter type')
                            param = param[init_len+1:]

                            param_cl = FClass.FiMParagraphParameters(param, param_type)
                            paragraph_parameters.append(param_cl)

                        paragraph_name = " ".join( name_split[:param_index] )
                        pass
                except:
                    raise Exception(f"Invalid paragraph parameter at line {index+1}")

                if (paragraph_parameters is not None or paragraph_return is not None) and paragraph_main:
                    raise Exception(f"Main paragraph cannot have parameter or return")

                continue
            elif re.match("That's all about", line):
                if not is_in_paragraph:
                    raise Exception(f"Unexpected paragraph ending at line {index+1}")
                is_in_paragraph = False
                if FMethods.StripEndPunctuation(FMethods.RemoveComment(line)) != f"That's all about {paragraph_name}":
                    raise Exception(f"Paragraph ending name not matching start at line {index+1}")

                self.Paragraphs[ paragraph_name ] = FClass.FiMParagraph( paragraph_index, index-1, paragraph_main, paragraph_parameters, paragraph_return )

                paragraph_name = None
                paragraph_main = False
                paragraph_parameters = None
                paragraph_return = None
                paragraph_index = -1
                continue

            # Actually. While at it, let's also grab the global variables
            if not is_in_paragraph:
                if line.startswith( FGlobal.Variable_Declaration ):
                    line = line.replace( FGlobal.Variable_Declaration, "", 1 )
                    line = FMethods.RemoveComment( line )
                    line = FMethods.StripEndPunctuation( line )
                    try:
                        variable_name, variable = FMethods.InterpretVariableDeclaration( line, self.Variables, self )
                    except Exception as e:
                        raise Exception(f"{e} at line {index+1}")
                    self.Variables[ variable_name ] = variable
                elif not FMethods.IsLineEmpty( line ):
                    raise Exception(f"Invalid line at line {index+1}")

        if is_in_paragraph:
            raise Exception(f"Unexpected end of report while scanning paragraphs")

        pass

    def GetParagraph(self, paragraph_name: str):
        """
            Returns the lines inside `paragraph_name`
        """
        if not paragraph_name in self.Paragraphs:
            raise Exception(f"Cannot find paragraph [{ paragraph_name }]")

        paragraph: FClass.FiMParagraph = self.Paragraphs[ paragraph_name ]
        paragraph_str = self._report_array[ paragraph.start_index:paragraph.end_index ]

        for index in range(0, len(paragraph_str)):
            paragraph_str[ index ] = FMethods.RemoveIndent( paragraph_str[index], 1 )

        return paragraph_str

    def RunParagraph(self, paragraph_name: str = None, param_object: list = None) -> (FClass.FiMVariable or None):

        # Default paragraph_name to the main paragraph
        if paragraph_name is None:
            paragraph_search = [k for k,v in self.Paragraphs.items() if v.isMain]
            if not paragraph_search[0]:
                raise Exception("Cannot find the main paragraph")
            paragraph_name = paragraph_search[0]

        if not paragraph_name in self.Paragraphs:
            raise Exception(f"Cannot find paragraph [{ paragraph_name }]")

        paragraph: FClass.FiMParagraph = self.Paragraphs[ paragraph_name ]

        variables_scope = dict()
        if paragraph.Parameters is not None and param_object is not None:
            param_copy = param_object.copy()
            # Null values if parameters is not enough
            while len(param_copy) < len(paragraph.Parameters):
                param_copy.append( FMethods.ConvertUnknownStringToProperValue("nothing", paragraph.Parameters[len(param_copy)].Type) )
            for index, value in enumerate(param_copy):
                if index >= len(paragraph.Parameters): break 
                variables_scope[ paragraph.Parameters[index].Name ] = FClass.FiMVariable( paragraph.Parameters[index].Type, False )
                if type(value) is list:
                    variables_scope[ paragraph.Parameters[index].Name ].setv( value.copy() )
                else:
                    variables_scope[ paragraph.Parameters[index].Name ].setv( value )

        _, return_variable = self.RunStatement(paragraph.start_index, paragraph.end_index, 1, variables_scope)

        if return_variable is None and paragraph.Return_Type is not None:
            raise Exception(f"Expected value return at paragraph {paragraph_name}")
        if return_variable and return_variable.Type != paragraph.Return_Type:
            raise Exception(f"Paragraph {paragraph_name} returned variable of different type")

        return return_variable

    # Returns the changed variables
    def RunStatement(self, start_index:int, end_index: int, indent_value: int = 1, variables_scope: dict = dict(), temp_variable: (str,FClass.FiMVariable) = None) -> (dict, object or None):
        index = start_index

        changed_variables = dict()
        scope_variables = {**variables_scope,**self.Variables}
        if temp_variable is not None:
            scope_variables[temp_variable[0]] = temp_variable[1]

        def analyze_method_result(method_result: FClass.FiMMethodReturn, index, line):
            # i really hate how i can't get that top index
            if method_result.type == FClass.FiMMethodReturnTypes.VARIABLE_INIT:
                scope_variables[ method_result.args["Variable Name"] ] = method_result.args["Variable"]
            elif method_result.type == FClass.FiMMethodReturnTypes.RETURN:
                return index, (changed_variables, method_result.args["Return Variable"])
            elif method_result.type == FClass.FiMMethodReturnTypes.RUN:
                paragraph_param: list = None
                if "Parameter" in method_result.args:
                    paragraph_param = method_result.args["Parameter"]
                self.RunParagraph( method_result.args["Paragraph Name"], paragraph_param)
            elif method_result.type == FClass.FiMMethodReturnTypes.ARRAY_MODIFY:
                if "Delete Variable" in method_result.args:
                    if method_result.args["Variable Name"] in self.Variables:
                        del self.Variables[ method_result.args["Variable Name"] ].getv()[ method_result.args["Slot"] ]
                    else:
                        del scope_variables[ method_result.args["Variable Name"] ].getv()[ method_result.args["Slot"] ]
                        del changed_variables[ method_result.args["Variable Name"] ]
                else:
                    if method_result.args["Variable Name"] in self.Variables:
                        self.Variables[ method_result.args["Variable Name"] ].setav( method_result.args["Slot"], method_result.args["Variable Value"] )
                    else:
                        scope_variables[ method_result.args["Variable Name"] ].setav( method_result.args["Slot"], method_result.args["Variable Value"] )
                        changed_variables[ method_result.args["Variable Name"] ] = scope_variables[ method_result.args["Variable Name"] ]
                
            elif (method_result.type == FClass.FiMMethodReturnTypes.VARIABLE_REPLACE or
                 method_result.type == FClass.FiMMethodReturnTypes.READ):
                if method_result.args["Variable Name"] in self.Variables:
                    self.Variables[ method_result.args["Variable Name"] ].setv( method_result.args["Variable Value"] )
                else:
                    scope_variables[ method_result.args["Variable Name"] ].setv( method_result.args["Variable Value"] )
                    changed_variables[ method_result.args["Variable Name"] ] = scope_variables[ method_result.args["Variable Name"] ]
            elif method_result.type == FClass.FiMMethodReturnTypes.D_BREAKPOINT:
                if 1>0:
                    pass
            elif method_result.type == FClass.FiMMethodReturnTypes.SWITCH:
                var_value = method_result.args["Variable Value"]

                has_switch_end = False
                s_final_index = index
                for s_index in range(index, end_index+1):
                    s_line = self._report_array[ s_index ]
                    s_line = FMethods.RemoveIndent( s_line, indent_value )
                    if s_line is None: continue
                    if not FMethods.IsLineEmpty( s_line ):
                        s_line = FMethods.RemoveComment( s_line ) # :(
                        s_line = FMethods.StripEndPunctuation( s_line )
                        if s_line == "That's what I did":
                            s_final_index = s_index
                            has_switch_end = True
                            break
                if not has_switch_end:
                    raise Exception("Unexpected EOP while checking switch end")

                #region
                # Sanity check
                s_cases = dict()
                _last_seen_case = None
                default_case = []
                for s_index in range(index+1, s_final_index):
                    s_line = self._report_array[ s_index ]
                    s_line = FMethods.RemoveIndent( s_line, indent_value+1 )
                    if s_line is None or FMethods.IsLineEmpty( s_line ):
                        if default_case:
                                default_case[1] += 1
                        else:
                            s_cases[ _last_seen_case ][1] += 1
                        continue

                    s_line = FMethods.RemoveComment( s_line ) # :(
                    if not s_line.endswith("..."):
                        raise Exception("Invalid case format (missing end elipsis)")
                    s_line = s_line[:len(s_line)-3]
                    
                    if s_line.startswith("On the ") and s_line.endswith(" hoof"):
                        if default_case:
                            raise Exception("End of switch statement expected")
                        case_key = s_line[ len("On the "): ]
                        case_key = case_key[:(len(case_key)-len(" hoof"))]
                        if FMethods._checkOrdinal(case_key):
                            case_key = int(case_key[:-2])
                        elif case_key in scope_variables:
                            if scope_variables[case_key].Type != FClass.FiMVariableTypes.NUMBER:
                                raise Exception("Expected number variable")
                            case_key = scope_variables[case_key].getv()
                        else:
                            raise Exception(f"Invalid case {case_key}")
                        if case_key in s_cases:
                            raise Exception(f"Case {case_key} already exists")
                        _last_seen_case = case_key
                        s_cases[ _last_seen_case ] = [s_index,0]
                    elif s_line == "If all else fails":
                        default_case = [s_index, 0]
                        
                if var_value in s_cases or default_case:
                    if var_value in s_cases:
                        if s_cases[var_value][1]==0:
                            return s_end_index, None
                        s_start_index = s_cases[var_value][0]+1
                        s_end_index = s_start_index + s_cases[var_value][1]-1
                    else:
                        if default_case[1]==0:
                            return s_end_index, None
                        s_start_index = default_case[0]+1
                        s_end_index = s_start_index + default_case[1]-1

                    # Execute
                    changed_vars, return_variable = self.RunStatement(s_start_index,s_end_index, indent_value+2, scope_variables)
                    if return_variable is not None:
                        return index, (changed_variables, return_variable)
                    # Update variables
                    for key,value in changed_vars.items():
                        if key in scope_variables:
                            scope_variables[key] = value
                            changed_variables[key] = value

                return s_final_index, None
                #endregion
            elif method_result.type == FClass.FiMMethodReturnTypes.WHILE:
                while_startIndex = index

                while_prefix = FMethods._findFirst( FGlobal.Methods['Loop While'], lambda x: line.startswith(f"{x} ") )
                ws_line = line[len(while_prefix):]

                has_while_end = False
                for w_index in range(index+1, end_index+1):
                    w_line = self._report_array[ w_index ]
                    w_line = FMethods.RemoveIndent( w_line, indent_value )
                    if w_line is None: continue
                    if not FMethods.IsLineEmpty( w_line ):
                        w_line = FMethods.RemoveComment( w_line ) # :(
                        w_line = FMethods.StripEndPunctuation( w_line )
                        if w_line == "That's what I did":
                            has_while_end = True
                            break
                if not has_while_end: raise Exception("Unexpected EOP while checking loop end")
                if w_index-1 < while_startIndex+1:
                    return w_index, None

                while True:
                    if FCalculation.FiMConditional.Parse(ws_line, scope_variables, self):

                        # Execute
                        changed_vars, return_variable = self.RunStatement(while_startIndex+1, w_index-1, indent_value+1, scope_variables)
                        if return_variable is not None:
                            return index, (changed_variables, return_variable)

                        # Update variables
                        for key,value in changed_vars.items():
                            if key in scope_variables:
                                scope_variables[key] = value
                                changed_variables[key] = value

                    else:
                        break
                
                return w_index, None
            elif method_result.type == FClass.FiMMethodReturnTypes.IF_ELSE:
                has_if_end = False
                i_end_index = index
                for i_index in range(index, end_index+1):
                    i_line = self._report_array[ i_index ]
                    i_line = FMethods.RemoveIndent( i_line, indent_value )
                    if i_line is None: continue
                    if not FMethods.IsLineEmpty( i_line ):
                        i_line = FMethods.RemoveComment( i_line ) # :(
                        i_line = FMethods.StripEndPunctuation( i_line )
                        if i_line == "That's what I would do":
                            i_end_index = i_index
                            has_if_end = True
                            break
                if not has_if_end: raise Exception("Unexpected EOP while checking if-else end")
                
                #region If-Else
                
                ### Phase 1. Identification
                f_indexes = list()
                for f_index in range(index, i_end_index):
                    f_line = self._report_array[ f_index ]
                    f_line = FMethods.RemoveIndent(f_line, indent_value)
                    if f_line is None or FMethods.IsLineEmpty( f_line ):
                        if len(f_indexes) > 0:
                            f_indexes[len(f_indexes)-1][3] += 1 # This might bite me in the butt someday
                        continue
                    f_line = FMethods.RemoveComment(f_line)
                    f_line = FMethods.StripEndPunctuation(f_line)

                    if f_line.endswith(" then"):
                        f_line = f_line[:-(len(" then"))]

                    # If
                    if f_line.startswith( FGlobal.Methods["If Statement"] ):
                        i_line = FMethods._findFirst( FGlobal.Methods["If Statement"], lambda x: f_line.startswith(x) )
                        f_indexes.append( [f_index, f_line[len(i_line):].strip(), 1, 0] )
                    # Else-If
                    elif FMethods._has( FGlobal.Methods["If Else Statement"],
                        lambda x: (FMethods._has( FGlobal.Methods["If Statement"],
                        lambda y: f_line.startswith(f"{x} {y.lower()}")))):
                        i_line = FMethods._findFirst( FGlobal.Methods["If Else Statement"], lambda x: f_line.startswith(x))
                        e_line = FMethods._findFirst( FGlobal.Methods["If Statement"], lambda x: f_line.startswith(f"{i_line} {x.lower()}"))
                        f_indexes.append( [f_index, f_line[len(i_line)+1+len(e_line):].strip(), 2, 0] )
                    # Else
                    elif f_line.startswith( FGlobal.Methods["If Else Statement"] ):
                        e_line = FMethods._findFirst(FGlobal.Methods["If Else Statement"], lambda x: f_line.startswith(x))
                        if len(f_line[len(e_line):].strip()) > 1:
                            raise Exception("Else statement expected to be empty")
                        f_indexes.append( [f_index, f_line[len(e_line):].strip(), 3, 0] )
                    else:
                        f_indexes[len(f_indexes)-1][3] += 1

                ### Phase 2. Check
                # If
                if len([f_i for f_i,f_l,f_t,_ in f_indexes if f_t==1]) != 1:
                    raise Exception("Expected only one If statement")
                if f_indexes[0][2] != 1:
                    raise Exception("Expected first statement to be If")
                # Else
                if len([f_i for f_i,f_l,f_t,_ in f_indexes if f_t==3]) == 1:
                    if f_indexes[-1][2] != 3:
                        raise Exception("Expected Else statement at end")
                elif len([f_i for f_i,f_l,f_t,_ in f_indexes if f_t==3]) > 1:
                    raise Exception("Expected only one Else statement")
                
                # letsa go :D
                f_list_index = 0
                for f_index, f_line, f_type, f_lines in f_indexes:
                    
                    has_passed = (f_type == 3) or FCalculation.FiMConditional.Parse(f_line, scope_variables, self)

                    if has_passed:
                        if f_lines == 0:
                            continue
                        s_start_index = f_index+1
                        s_end_index = f_index+f_lines

                        # Execute
                        changed_vars, return_variable = self.RunStatement(s_start_index,s_end_index, indent_value+1, scope_variables)
                        if return_variable is not None:
                            return index, (changed_variables, return_variable)

                        # Update variables
                        for key,value in changed_vars.items():
                            if key in scope_variables:
                                scope_variables[key] = value
                                changed_variables[key] = value

                        break

                    f_list_index += 1
                
                #endregion

                return i_end_index, None
            elif method_result.type == FClass.FiMMethodReturnTypes.FOR_COUNT:
                has_for_end = False
                f_final_index = index
                for f_index in range(index, end_index+1):
                    f_line = self._report_array[ f_index ]
                    f_line = FMethods.RemoveIndent( f_line, indent_value )
                    if f_line is None: continue
                    if not FMethods.IsLineEmpty( f_line ):
                        f_line = FMethods.RemoveComment( f_line ) # :(
                        f_line = FMethods.StripEndPunctuation( f_line )
                        if f_line == "That's what I did":
                            f_final_index = f_index
                            has_for_end = True
                            break
                if not has_for_end:
                    raise Exception("Unexpected EOP while checking for loop end")
                if f_final_index-1 < index+1:
                    return f_final_index, None

                f_name = method_result.args["Variable Name"]
                f_min = method_result.args["Minimum Value"]
                f_max = method_result.args["Maximum Value"]
                for value in range(f_min,f_max+1):
                    f_var: FClass.FiMVariable = FClass.FiMVariable( FClass.FiMVariableTypes.NUMBER, True )
                    f_var.setv( value )
                    
                    changed_vars, return_variable = self.RunStatement(index+1, f_final_index-1, indent_value+1, scope_variables, (f_name, f_var))
                    if return_variable is not None:
                        return index, (changed_variables, return_variable)
                    # Update variables
                    for key,value in changed_vars.items():
                        if key in scope_variables:
                            scope_variables[key] = value
                            changed_variables[key] = value
                return f_final_index, None
            elif method_result.type == FClass.FiMMethodReturnTypes.FOR_ITER:
                has_for_end = False
                f_final_index = index
                for f_index in range(index, end_index+1):
                    f_line = self._report_array[ f_index ]
                    f_line = FMethods.RemoveIndent( f_line, indent_value )
                    if f_line is None: continue
                    if not FMethods.IsLineEmpty( f_line ):
                        f_line = FMethods.RemoveComment( f_line ) # :(
                        f_line = FMethods.StripEndPunctuation( f_line )
                        if f_line == "That's what I did":
                            f_final_index = f_index
                            has_for_end = True
                            break
                if not has_for_end:
                    raise Exception("Unexpected EOP while checking for loop end")
                if f_final_index-1 < index+1:
                    return f_final_index, None

                f_iterable: FClass.FiMVariable = method_result.args["Iterable"]
                f_name = method_result.args["Variable Name"]
                if f_iterable.Type == FClass.FiMVariableTypes.STRING:
                    for value in f_iterable.getsv():
                        f_var: FClass.FiMVariable = FClass.FiMVariable( FClass.FiMVariableTypes.GetArrayType(f_iterable.Type), True )
                        f_var.setv( f"\'{value}\'" )

                        changed_vars, return_variable = self.RunStatement(index+1, f_final_index-1, indent_value+1, scope_variables, (f_name, f_var))
                        if return_variable is not None:
                            return index, (changed_variables, return_variable)
                        # Update variables
                        for key,value in changed_vars.items():
                            if key in scope_variables:
                                scope_variables[key] = value
                                changed_variables[key] = value
                else:
                    for _,value in f_iterable.getv().items():
                        f_var: FClass.FiMVariable = FClass.FiMVariable( FClass.FiMVariableTypes.GetArrayType(f_iterable.Type), True )
                        f_var.setv( value )

                        changed_vars, return_variable = self.RunStatement(index+1, f_final_index-1, indent_value+1, scope_variables, (f_name, f_var))
                        if return_variable is not None:
                            return index, (changed_variables, return_variable)
                        # Update variables
                        for key,value in changed_vars.items():
                            if key in scope_variables:
                                scope_variables[key] = value
                                changed_variables[key] = value
                    
                
                return f_final_index, None
            
            return index, None

        """
        print("== START SCOPE ==")
        for scope_index in range( index, end_index + 1 ):
            print( self._report_array[ scope_index ] )
        print("== END SCOPE ==")
        """

        while index <= end_index:
            line = self._report_array[ index ]
            if FMethods.IsLineEmpty( line ):
                index += 1
                continue
            line = FMethods.RemoveIndent( line, indent_value )
            if line is None:
                print( self._report_array[ index ] )
                raise Exception(f"Invalid indentation at line {index+1}")
            line = FMethods.RemoveComment( line ) # :(
            r_line = line
            line = FMethods.StripEndPunctuation( line )

            # Code here
            method_result = FMethods.ScanMethod( self, line, scope_variables, r_line )

            index, return_args = analyze_method_result( method_result, index, line )
            if return_args is not None:
                return return_args[0], return_args[1]

            index += 1

        return changed_variables, None