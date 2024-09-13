from gimpfu import *

def texture_process(image, drawable):

    width = pdb.gimp_image_width(image)
    height = pdb.gimp_image_height(image)
    new_size = max(width, height)

    pdb.gimp_image_resize(image, new_size, new_size, (new_size - width) // 2, (new_size - height) // 2)

    pdb.gimp_context_set_interpolation(INTERPOLATION_CUBIC)
    pdb.gimp_image_scale(image, 512, 512)

    pdb.gimp_context_set_interpolation(INTERPOLATION_NONE)
    pdb.gimp_image_scale(image, 256, 256)

    layer = pdb.gimp_image_get_active_layer(image)
    pdb.plug_in_hsv_noise(image, layer, 8, 0, 255, 76)  # (Holdness=8, Hue=0, Saturation=1, Value=0.3)

    pdb.gimp_image_convert_indexed(image, CONVERT_DITHER_NONE, CONVERT_PALETTE_WEB, 0, False, False, "")

    offset_x = (layer.width - pdb.gimp_image_width(image)) // 2
    offset_y = (layer.height - pdb.gimp_image_height(image)) // 2
    pdb.gimp_image_resize(image, layer.width, layer.height, offset_x, offset_y)

    pdb.gimp_displays_flush()

register(
    "crunchy_texture_process",
    "Process a seamless texture with scaling, noise, and indexed color conversion",
    "Automates the texture processing workflow",
    "Your Name",
    "Your License",
    "2025",
    "<Image>/Filters/Custom/Crunchy Texture Process",
    "RGB*, GRAY*",
    [],
    [],
    texture_process
)

main()
