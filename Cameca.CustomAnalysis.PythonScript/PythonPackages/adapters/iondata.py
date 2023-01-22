import numpy as np
import ctypes
import System


def prepare_section_data(iondata, section_name, sec_dtype):
    """ISectionInfo instance to appropriately sized np.ndarray"""
    row_count = iondata.IonCount
    section_info = iondata.Sections[section_name];
    return np.empty(section_info.ValuesPerRecord * row_count, dtype=sec_dtype)


def fill_array(array, dtype, buffer_length, lng_ptr, chunk_offset):
    ctype = np.ctypeslib.as_ctypes_type(dtype)
    ptr = (ctype * buffer_length).from_address(lng_ptr);
    buffer_ = np.ctypeslib.as_array(ptr);
    array[chunk_offset:chunk_offset + buffer_length] = buffer_


def reshape_array(array, records, values_per_record):
    array.shape = (records, values_per_record)
